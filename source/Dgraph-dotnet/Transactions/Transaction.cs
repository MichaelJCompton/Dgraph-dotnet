using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Client = client;

            TransactionState = TransactionState.OK;

            Context = new TxnContext();
        }

        public async Task<FluentResults.Result<string>> Query(string queryString) {
            return await QueryWithVars(queryString, new Dictionary<string, string>());
        }

        public async Task<FluentResults.Result<string>> QueryWithVars(string queryString, Dictionary<string, string> varMap) {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail<string>(new TransactionFinished(TransactionState.ToString()));
            }

            try {
                Api.Request request = new Api.Request();
                request.Query = queryString;
                request.Vars.Add(varMap);
                request.StartTs = Context.StartTs;

                var queryResponse = await Client.Query(request);

                var err = MergeContext(queryResponse.Txn);

                if (err.IsSuccess) {
                    return Results.Ok<string>(queryResponse.Json.ToStringUtf8());
                } else {
                    return err.ToResult<string>();
                }

            } catch (RpcException rpcEx) {
                return Results.Fail<string>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        public async Task<FluentResults.Result<DgraphSchema>> SchemaQuery() {
            return await SchemaQuery("schema { }");
        }

        public async Task<FluentResults.Result<DgraphSchema>> SchemaQuery(string schemaQuery) {
            AssertNotDisposed();

            if (!schemaQuery.Trim().StartsWith("schema")) {
                return Results.Fail<DgraphSchema>("Not a schema query.");
            }

            var result = await Query(schemaQuery);
            if (result.IsFailed) {
                return result.ToResult<DgraphSchema>();
            }

            try {
                return Results.Ok<DgraphSchema>(JsonConvert.DeserializeObject<DgraphSchema>(result.Value));
            } catch (Exception ex) {
                return Results.Fail<DgraphSchema>(new FluentResults.ExceptionalError(ex));
            }
        }

        public async Task<FluentResults.Result<IDictionary<string, string>>> Mutate(string json) {
            AssertNotDisposed();

            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8(json);
            return await Mutate(mut);
        }

        public async Task<FluentResults.Result> Delete(string json) {
            AssertNotDisposed();

            var mut = new Api.Mutation();
            mut.DeleteJson = Google.Protobuf.ByteString.CopyFromUtf8(json);
            return (await Mutate(mut)).ToResult();
        }

        internal async Task<FluentResults.Result<IDictionary<string, string>>> Mutate(Api.Mutation mutation) {
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
			    Api.Request req = new Api.Request();
			    req.Mutations.Add(mutation);

                req.StartTs = Context.StartTs;
                var response = await Client.Mutate(req);

                if (mutation.CommitNow) {
                    TransactionState = TransactionState.Committed;
                }

                var err = MergeContext(response.Txn);
                if (err.IsFailed) {
                    return err.ToResult<IDictionary<string, string>>();
                }

                return Results.Ok<IDictionary<string, string>>(response.Uids);

            } catch (RpcException rpcEx) {

                // From Dgraph code txn.go
                //
                // Since a mutation error occurred, the txn should no longer be used
                // (some mutations could have applied but not others, but we don't know
                // which ones).  Discarding the transaction enforces that the user
                // cannot use the txn further.
                await Discard(); // Ignore error - user should see the original error.

                TransactionState = TransactionState.Error; // overwrite the aborted value
                return Results.Fail<IDictionary<string, string>>(new FluentResults.ExceptionalError(rpcEx));
            }
        }

        // Must be ok to call multiple times!
        public async Task Discard() {
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
                    await Client.Discard(Context);
                } catch (RpcException) {
                    // Eat it ... nothing else to do? Dgraph will clean up eventually?
                }
            }
        }

        public async Task<FluentResults.Result> Commit() {
            AssertNotDisposed();

            if (TransactionState != TransactionState.OK) {
                return Results.Fail(new TransactionFinished(TransactionState.ToString()));
            }

            TransactionState = TransactionState.Committed;

            if (!HasMutated) {
                return Results.Ok();
            }

            try {
                await Client.Commit(Context);
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
            if (srcContext == null) {
                return Results.Ok();
            }

            if (Context.StartTs == 0) {
                Context.StartTs = srcContext.StartTs;
            }

            if (Context.StartTs != srcContext.StartTs) {
                return Results.Fail(new StartTsMismatch());
            }

            Context.Keys.Add(srcContext.Keys);
            Context.Preds.Add(srcContext.Preds);

            return Results.Ok();
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

            if (!Disposed && TransactionState == TransactionState.OK) {
                Disposed = true;

                Task.Run(() => Discard());
                //
                // This makes it just run in another thread?  So this thread
                // runs off an gets back to work and we don't really care how
                // the Discard() went.  But can this race with disposal of
                // everything?  See how it goes, but maybe nothing should be
                // done here and we just expect Dgraph to clean up?
            }
        }

        #endregion
    }
}