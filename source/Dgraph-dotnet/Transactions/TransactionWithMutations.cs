using System.Collections.Generic;
using DgraphDotNet;

namespace DgraphDotNet.Transactions {

    internal class TransactionWithMutations : Transaction, ITransactionWithMutations {

        internal TransactionWithMutations(DgraphMutationsClient client) : base(client) {

        }

        public IMutation NewMutation() {
            AssertNotDisposed();

            return new Mutation();
        }

        public FluentResults.Result<IDictionary<string, string>> ApiMutate(Api.Mutation mutation) {
            AssertNotDisposed();
            
            return base.Mutate(mutation);
        }

    }
}