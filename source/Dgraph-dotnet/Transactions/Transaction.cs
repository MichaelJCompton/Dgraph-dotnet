using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Api;
using DgraphDotNet.Schema;
using FluentResults;
using Grpc.Core;
using Newtonsoft.Json;

namespace DgraphDotNet.Transactions {

    internal class Transaction : ITransaction {

        private readonly IDgraphClientInternal Client;

        public TransactionState TransactionState { get; private set; }

        private readonly TxnContext Context;
        private bool HasMutated;

        internal Transaction(IDgraphClientInternal client) {
            this.Client = client;

            TransactionState = TransactionState.OK;

            Context = new TxnContext();
            Context.LinRead = client.GetLinRead();
        }

        public FluentResults.Result<string> Query(string queryString) {
            return QueryWithVars(queryString, new Dictionary<string, string>());
        }

        public FluentResults.Result<string> QueryWithVars(string queryString, Dictionary<string, string> varMap) {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail<string>(new TransactionFinished(TransactionState.ToString()));
            }

            try {
                Api.Request request = new Api.Request();
                request.Query = queryString;
                request.Vars.Add(varMap);
                request.StartTs = Context.StartTs;
                request.LinRead = Context.LinRead;

                var queryResponse = Client.Query(request);

                var err = MergeContext(queryResponse.Txn);

                if (err.IsSuccess) {
                    return Results.Ok<string>(queryResponse.Json.ToStringUtf8());
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
            AssertNotDisposed();

            if(!schemaQuery.Trim().StartsWith("schema")) {
                return Results.Fail<DgraphSchema>("Not a schema query.");
            }

            var result = Query(schemaQuery);
            if (result.IsFailed) {
                return result.ConvertToResultWithValueType<DgraphSchema>();
            }

            try {
                return Results.Ok<DgraphSchema>(JsonConvert.DeserializeObject<DgraphSchema>(result.Value));
            } catch (Exception ex) {
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

            if (TransactionState != TransactionState.OK) {
                return Results.Fail<IDictionary<string, string>>(new TransactionFinished(TransactionState.ToString()));
            }

            if (mutation.Del.Count == 0
                && mutation.DeleteJson.Length == 0
                && mutation.Set.Count == 0
                && mutation.SetJson.Length == 0) {
                return Results.Ok<IDictionary<string, string>>(new Dictionary<string, string>());
            }

            HasMutated = true;

            try {
                mutation.StartTs = Context.StartTs;
                var assigned = Client.Mutate(mutation);

                if (mutation.CommitNow) {
                    TransactionState = TransactionState.Committed;
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

                TransactionState = TransactionState.Error; // overwrite the aborted value
                return Results.Fail<IDictionary<string, string>>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        // Must be ok to call multiple times!
        public void Discard() {
            if (TransactionState == TransactionState.Committed) {
                return;
            }

            if (TransactionState != TransactionState.Aborted) {
                TransactionState = TransactionState.Aborted;

                if (!HasMutated) {
                    return;
                }

                Context.Aborted = true;

                try {
                    Client.Discard(Context);
                } catch (RpcException) {
                    // Eat it ... nothing else to do? Dgraph will clean up eventually?
                }
            }
        }

        public FluentResults.Result Commit() {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail(new TransactionFinished(TransactionState.ToString()));
            }

            TransactionState = TransactionState.Committed;

            if (!HasMutated) {
                return Results.Ok();
            }

            try {
                Client.Commit(Context);
                return Results.Ok();
            } catch (RpcException rpcEx) {
                // I'm not 100% sure here - so what happens if the transaction
                // throws an exception?  It'll be in state committed, but it
                // isn't in Dgraph.  It can't be retried because of the state.
                // The handling here is the same as in Dgo, so I assume you have
                // to retry all the operations again and then try to commit
                // again.
                return Results.Fail(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        #region dgraphtransaction

        private FluentResults.Result MergeContext(TxnContext srcContext) {
            if (Context == null) {
                return Results.Ok();
            }

            MergeLinReads(Context.LinRead, srcContext.LinRead);
            Client.MergeLinRead(srcContext.LinRead);

            if (Context.StartTs == 0) {
                Context.StartTs = srcContext.StartTs;
            }

            if (Context.StartTs != srcContext.StartTs) {
                return Results.Fail(new StartTsMismatch());
            }

            Context.Keys.Add(srcContext.Keys);

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

        private bool Disposed;

        protected void AssertNotDisposed() {
            if (Disposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose() {
            Disposed = true;

            Discard(); // like all dispose interface calls, it's safe to call Discard() many times
            //
            // might need to allow some time here cause there's a deadline on
            // object clean up.  So I might need to set a deadline that gets
            // passed to the backend call so we don't wait on the Dgraph call to
            // succeed?
        }

        #endregion
    }
}