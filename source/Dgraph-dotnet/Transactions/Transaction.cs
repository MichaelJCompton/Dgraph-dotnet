using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Api;
using DgraphDotNet.Schema;
using FluentResults;
using Grpc.Core;
using Newtonsoft.Json;

/*
I'd like to write
        
using(txn = client.NewTransaction()) {
    txn.mutate
    txn.query
    txn.mutate
    txn.commit
} 

or

txn = client.NewTransaction()
txn.mutate()
if(...) {
    txn.commit();
} else {
    txn.discard();
}

or

client.Query(....) without a transaction  ??
*/

namespace DgraphDotNet.Transactions {

    internal class Transaction : ITransaction {

        private readonly IDgraphClientInternal client;

        private enum TransactionState { OK, Committed, Aborted, Error }

        TransactionState transactionState = TransactionState.OK;

        TxnContext context;
        bool hasMutated;

        Response lastQueryResponse;

        internal Transaction(IDgraphClientInternal client) {
            this.client = client;

            context = new TxnContext();
            context.LinRead = client.GetLinRead();
        }

        public bool Committed => transactionState == TransactionState.Committed;
        public bool Aborted => transactionState == TransactionState.Aborted;
        public bool HasError => transactionState == TransactionState.Error;
        public bool IsOK => transactionState == TransactionState.OK;

        // FIXME: 
        // public blaa SchemaQuery(string queryString) {
        //     ???
        // }

        public FluentResults.Result<string> Query(string queryString) {
            AssertNotDisposed();

            return QueryWithVars(queryString, new Dictionary<string, string>());
        }

        public FluentResults.Result<string> QueryWithVars(string queryString, Dictionary<string, string> varMap) {
            AssertNotDisposed();

            if (transactionState != TransactionState.OK) {
                return Results.Fail<string>(new TransactionFinished(transactionState.ToString()));
            }

            try {
                Api.Request request = new Api.Request();
                request.Query = queryString;
                request.Vars.Add(varMap);
                request.StartTs = context.StartTs;
                request.LinRead = context.LinRead;

                lastQueryResponse = client.Query(request);

                var err = MergeContext(lastQueryResponse.Txn);

                if (err.IsSuccess) {
                    return Results.Ok<string>(lastQueryResponse.Json.ToStringUtf8());
                } else {
                    return err.ConvertToResultWithValueType<string>();
                }

            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public FluentResults.Result<DgraphSchema> SchemaQuery() {
            return SchemaQuery("schema { }");
        }

        public FluentResults.Result<DgraphSchema> SchemaQuery(string schemaQuery) {
            var result = Query(schemaQuery);
            if (result.IsFailed) {
                return result.ConvertToResultWithValueType<DgraphSchema>();
            }

            // Should never fail a valid schema query (tests should ensure all
            // cases are handled), but there's no protetection to ensure that
            // the schemaQuery was actually a schema query, so we should wrap
            // for parsing errors.
            try {
                return Results.Ok<DgraphSchema>(JsonConvert.DeserializeObject<DgraphSchema>(result.Value));
            } catch(Exception ex) {
                return Results.Fail<DgraphSchema>(new FluentResults.ExceptionalError(ex));
            }
        }

        public FluentResults.Result<IDictionary<string, string>> Mutate(string json) {
            AssertNotDisposed();

            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8(json);
            return Mutate(mut);
        }

        public FluentResults.Result Delete(string json) {
            AssertNotDisposed();

            var mut = new Api.Mutation();
            mut.DeleteJson = Google.Protobuf.ByteString.CopyFromUtf8(json);
            return Mutate(mut).ConvertTo();
        }

        internal FluentResults.Result<IDictionary<string, string>> Mutate(Api.Mutation mutation) {
            AssertNotDisposed();

            if (transactionState != TransactionState.OK) {
                return Results.Fail<IDictionary<string, string>>(new TransactionFinished(transactionState.ToString()));
            }

            if (mutation.Del.Count == 0
                && mutation.DeleteJson.Length == 0
                && mutation.Set.Count == 0
                && mutation.SetJson.Length == 0) {
                return Results.Ok<IDictionary<string, string>>(new Dictionary<string, string>());
            }

            hasMutated = true;

            try {
                mutation.StartTs = context.StartTs;
                var assigned = client.Mutate(mutation);

                if (mutation.CommitNow) {
                    transactionState = TransactionState.Committed;
                }

                var err = MergeContext(assigned.Context);
                if (err.IsFailed) {
                    return err.ConvertToResultWithValueType<IDictionary<string, string>>();
                }

                return Results.Ok<IDictionary<string, string>>(assigned.Uids);

            } catch (RpcException rpcEx) {

                // From Dgraph code txn.go
                //
                // Since a mutation error occurred, the txn should no longer be used
                // (some mutations could have applied but not others, but we don't know
                // which ones).  Discarding the transaction enforces that the user
                // cannot use the txn further.
                Discard(); // Ignore error - user should see the original error.

                transactionState = TransactionState.Error; // overwrite the aborted value
                return Results.Fail<IDictionary<string, string>>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public void Discard() {
            if (transactionState != TransactionState.OK) {
                return;
            }

            transactionState = TransactionState.Aborted;

            if (!hasMutated) {
                return;
            }

            context.Aborted = true;

            try {
                client.Discard(context);
            } catch (RpcException) { }
        }

        public FluentResults.Result Commit() {
            AssertNotDisposed();

            if (transactionState != TransactionState.OK) {
                return Results.Fail(new TransactionFinished(transactionState.ToString()));
            }

            transactionState = TransactionState.Committed;

            if (!hasMutated) {
                return Results.Ok();
            }

            try {
                client.Commit(context);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        #region dgraphtransaction

        private FluentResults.Result MergeContext(TxnContext srcContext) {
            if (context == null) {
                return Results.Ok();
            }

            MergeLinReads(context.LinRead, srcContext.LinRead);
            client.MergeLinRead(srcContext.LinRead);

            if (context.StartTs == 0) {
                context.StartTs = srcContext.StartTs;
            }

            if (context.StartTs != srcContext.StartTs) {
                return Results.Fail(new StartTsMismatch());
            }

            context.Keys.Add(srcContext.Keys);

            return Results.Ok();
        }

        internal static void MergeLinReads(LinRead dst, LinRead src) {
            if (src == null || src.Ids == null) {
                return;
            }

            foreach (var entry in src.Ids) {
                if (dst.Ids.TryGetValue(entry.Key, out var did) && (did >= entry.Value)) {
                    // do nothing
                } else {
                    dst.Ids[entry.Key] = entry.Value;
                }
            }
        }

        #endregion

        // 
        // ------------------------------------------------------
        //              disposable pattern.
        // ------------------------------------------------------
        //
        #region disposable pattern

        private bool disposed => Aborted;

        /// <summary>
        /// Asserts that instance is not disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the client has been disposed.</exception>
        protected void AssertNotDisposed() {
            if (disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            Discard(); // like all dispose interface calls, it's safe to call Discard() many times
        }

        #endregion
    }
}