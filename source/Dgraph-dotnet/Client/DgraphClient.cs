using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Api;
using DgraphDotNet.Schema;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using FluentResults;
using Grpc.Core;
using System.Threading.Tasks;

namespace DgraphDotNet {

    internal class DgraphClient : IDgraphClient, IDgraphClientInternal {

        protected readonly IGRPCConnectionFactory connectionFactory;
        protected readonly ITransactionFactory transactionFactory;

        internal DgraphClient(IGRPCConnectionFactory connectionFactory, ITransactionFactory transactionFactory) {
            this.connectionFactory = connectionFactory;
            this.transactionFactory = transactionFactory;
        }

        // 
        // ------------------------------------------------------
        //                   Connections
        // ------------------------------------------------------
        //

        #region Connections

        private readonly ConcurrentDictionary<string, IGRPCConnection> connections = new ConcurrentDictionary<string, IGRPCConnection>();

        public void Connect(string address, ChannelCredentials credentials = null, IEnumerable<ChannelOption> options = null) {
            AssertNotDisposed();

            if (!string.IsNullOrEmpty(address)) {
                if (connections.TryGetValue(address, out IGRPCConnection connection)) {
                    connection.LastKnownStatus = Status.DefaultSuccess;
                } else {
                    if (connectionFactory.TryConnect(address, out connection, credentials, options)) {
                        if (!connections.TryAdd(address, connection)) {
                            // another thread wrote in between.  That means there 
                            // is already a connection for this address, so 
                            // dispose and ignore this connection.
                            connection.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// All connections (OK or failed) that have been submitted to the 
        /// client and not disconnected.
        /// </summary>
        public IEnumerable<string> AllConnections() {
            AssertNotDisposed();

            return connections.Select(kvp => kvp.Key);
        }

        #endregion

        // 
        // ------------------------------------------------------
        //                   Transactions
        // ------------------------------------------------------
        //

        #region transactions

        private Random rnd = new Random();

        public async Task<FluentResults.Result> AlterSchema(string newSchema) {
            AssertNotDisposed();

            var op = new Api.Operation();
            op.Schema = newSchema;

            try {
                await connections.Values.ElementAt(rnd.Next(connections.Count)).Alter(op);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result> DropAll() {
            AssertNotDisposed();

            var op = new Api.Operation() {
                DropAll = true
            };

            try {
                await connections.Values.ElementAt(rnd.Next(connections.Count)).Alter(op);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result<string>> CheckVersion() {
            AssertNotDisposed();

            try {
                var versionResult = await connections.Values.ElementAt(rnd.Next(connections.Count)).CheckVersion();
                return Results.Ok<string>(versionResult.Tag);
            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result<DgraphSchema>> SchemaQuery() {
            return await SchemaQuery("schema { }");
        }

        public async Task<FluentResults.Result<DgraphSchema>> SchemaQuery(string schemaQuery) {
            AssertNotDisposed();
            
            using(var transaction = NewTransaction()) {
                return await transaction.SchemaQuery(schemaQuery);
            }
        }

        public async Task<FluentResults.Result<string>> Query(string queryString) {
            AssertNotDisposed();

            return await QueryWithVars(queryString, new Dictionary<string, string>());
        }

        public async Task<FluentResults.Result<string>> QueryWithVars(string queryString, Dictionary<string, string> varMap) {
            using(var transaction = NewTransaction()) {
                return await transaction.QueryWithVars(queryString, varMap);
            }
        }

        public ITransaction NewTransaction() {
            AssertNotDisposed();

            return transactionFactory.NewTransaction(this);
        }

        public async Task<FluentResults.Result<(INode, bool)>> Upsert(string predicate, GraphValue value, int maxRetrys = 1) {
            AssertNotDisposed();

            var query = $"{{ q(func: eq({predicate}, \"{value.ToString()}\")) {{ uid }} }}";
            var newNodeBlankName = "upsertNode";

            var retryRemaining = (maxRetrys < 1) ? 1 : maxRetrys;
            FluentResults.Result<(INode, bool)> result = null;

            Func<FluentResults.Result<(INode, bool)>, FluentResults.Result<(INode, bool)>, FluentResults.Result<(INode, bool)>> addErr =
                (FluentResults.Result<(INode, bool)> curError, FluentResults.Result<(INode, bool)> newError) => {
                    return curError == null || !curError.IsFailed
                        ? newError
                        : Results.Merge<(INode, bool)>(curError, newError);
                };

            while (retryRemaining >= 0) {
                retryRemaining--;

                using(var txn = NewTransaction()) {
                    var queryResult = await txn.Query(query);

                    if (queryResult.IsFailed) {
                        result = addErr(result, queryResult.ToResult<(INode, bool)>());
                        continue;
                    }

                    if (String.Equals(queryResult.Value, "{\"q\":[]}", StringComparison.Ordinal)) {
                        var assigned = await txn.Mutate($"{{ \"uid\": \"_:{newNodeBlankName}\", \"{predicate}\": \"{value.ToString()}\" }}");
                        if (assigned.IsFailed) {
                            result = addErr(result, assigned.ToResult<(INode, bool)>());
                            continue;
                        }
                        var err = await txn.Commit();
                        if (err.IsSuccess) {
                            var UIDasString = assigned.Value[newNodeBlankName].Replace("0x", string.Empty); // why doesn't UInt64.TryParse() work with 0x...???
                            if (UInt64.TryParse(UIDasString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var UID)) {
                                return Results.Ok<(INode, bool)>((new UIDNode(UID),false));
                            }
                            result = addErr(result, Results.Fail<(INode, bool)>("Failed to parse UID : " + UIDasString));
                            continue;
                        }
                        result = addErr(result, err.ToResult<(INode, bool)>());
                        continue;
                    } else {
                        var UIDasString = queryResult.Value
                            .Replace("{\"q\":[{\"uid\":\"0x", string.Empty)
                            .Replace("\"}]}", string.Empty);

                        if (UInt64.TryParse(UIDasString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var UID)) {
                            return Results.Ok<(INode, bool)>((new UIDNode(UID),true));
                        }
                        result = addErr(result, Results.Fail<(INode, bool)>("Failed to parse UID : " + UIDasString));
                        continue;
                    }
                }
            }
            return result;
        }

        public async Task<Response> Query(Api.Request req) {
            AssertNotDisposed();

            return await connections.Values.ElementAt(rnd.Next(connections.Count)).Query(req);
        }

        public async Task<Assigned> Mutate(Api.Mutation mut) {
            AssertNotDisposed();

            return await connections.Values.ElementAt(rnd.Next(connections.Count)).Mutate(mut);
        }

        public async Task Commit(TxnContext context) {
            AssertNotDisposed();

            await connections.Values.ElementAt(rnd.Next(connections.Count)).Commit(context);
        }

        public async Task Discard(TxnContext context) {
            AssertNotDisposed();

            await connections.Values.ElementAt(rnd.Next(connections.Count)).Discard(context);
        }

        #endregion

        // 
        // ------------------------------------------------------
        //              disposable pattern.
        // ------------------------------------------------------
        //
        #region disposable pattern

        // see disposable pattern at : https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
        // and http://reedcopsey.com/tag/idisposable/
        //
        // Trying to follow the rules here 
        // https://blog.stephencleary.com/2009/08/second-rule-of-implementing-idisposable.html
        // for all the dgraph dispose bits
        //
        // For this class, it has only managed IDisposable resources, so it just needs to call the Dispose()
        // of those resources.  It's safe to have nothing else, because IDisposable.Dispose() must be safe to call
        // multiple times.  Also don't need a finalizer.  So this simplifies the general pattern, which isn't needed here.

        bool disposed; // = false;
        protected bool Disposed => disposed;

        protected void AssertNotDisposed() {
            if (Disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            DisposeIDisposables();
        }

        protected virtual void DisposeIDisposables() {
            if (!Disposed) {
                this.disposed = true; // throw ObjectDisposedException on calls to client if it has been disposed. 
                foreach (var con in connections.Values) {
                    con.Dispose();
                }
            }
        }

        #endregion
    }
}