using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using Grpc.Core;

/*
 *
 *  service Dgraph {
 *	  rpc Query (Request)            returns (Response) {}
 *    rpc Mutate (Mutation)          returns (Assigned) {}
 *    rpc Alter (Operation)          returns (Payload) {}
 *    rpc CommitOrAbort (TxnContext) returns (TxnContext) {}
 *    rpc CheckVersion(Check)        returns (Version) {}
 *  }
 *
 */

// For unit testing.  Allows to make mocks of the internal interfaces and factories
// so can test in isolation from a Dgraph instance.
//
// When I put this in an AssemblyInfo.cs it wouldn't compile any more.
[assembly : System.Runtime.CompilerServices.InternalsVisibleTo("Dgraph-dotnet.tests")]
[assembly : System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // for NSubstitute

namespace DgraphDotNet {

    internal class DgraphClient : IDgraphClient {

        protected readonly GRPCConnectionFactory connectionFactory;

        internal DgraphClient(GRPCConnectionFactory connectionFactory) {
            this.connectionFactory = connectionFactory;
            linRead = new LinRead();
        }

        // 
        // ------------------------------------------------------
        //                   Connections
        // ------------------------------------------------------
        //
        #region Connections

        private readonly ConcurrentDictionary<string, GRPCConnection> connections = new ConcurrentDictionary<string, GRPCConnection>();

        public void Connect(string address) {
            AssertNotDisposed();

            if (!string.IsNullOrEmpty(address)) {
                if (connections.TryGetValue(address, out GRPCConnection connection)) {
                    connection.LastKnownStatus = Status.DefaultSuccess;
                } else {
                    if (connectionFactory.TryConnect(address, out connection)) {
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

        // FIXME: Needs option the connections for retries, timeout etc.

        #endregion

        // 
        // ------------------------------------------------------
        //                   Transactions
        // ------------------------------------------------------
        //
        #region transactions

        private LinRead linRead;
        private readonly System.Object linReadMutex = new System.Object();
        private System.Object LinReadMutex => linReadMutex;
        private Random rnd = new Random();

        public void AlterSchema(string newSchema) {
            AssertNotDisposed();

            var op = new Api.Operation();
            op.Schema = newSchema;
            connections.Values.FirstOrDefault().Alter(op);
        }

        public virtual ITransaction NewTransaction() {
            AssertNotDisposed();

            return new Transaction<DgraphClient>(this);
        }

        internal Response Query(Api.Request req) {
            AssertNotDisposed();

            return connections.Values.ElementAt(rnd.Next(connections.Count)).Query(req);
        }

        internal Assigned Mutate(Api.Mutation mut) {
            AssertNotDisposed();

            return connections.Values.ElementAt(rnd.Next(connections.Count)).Mutate(mut);
        }

        internal void Commit(TxnContext context) {
            AssertNotDisposed();

            connections.Values.ElementAt(rnd.Next(connections.Count)).Commit(context);
        }

        internal void Discard(TxnContext context) {
            AssertNotDisposed();

            connections.Values.ElementAt(rnd.Next(connections.Count)).Discard(context);
        }

        internal LinRead GetLinRead() {
            LinRead lr;

            lock(LinReadMutex) {
                lr = new LinRead(linRead);
            }
            return lr;
        }

        internal void MergeLinRead(LinRead newLinRead) {
            lock(LinReadMutex) {
                Transaction<DgraphClient>.MergeLinReads(linRead, newLinRead);
            }
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

        /// <summary>
        /// Asserts that instance is not disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Thrown if the client has been disposed.</exception>
        protected void AssertNotDisposed() {
            if (Disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Close all connections.  It is an error to call any client functions after a call to 
        /// <c>Dispose()</c> and such calls result in an ObjectDisposedException.
        /// </summary>
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