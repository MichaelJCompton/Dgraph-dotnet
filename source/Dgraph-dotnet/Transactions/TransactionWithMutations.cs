using DgraphDotNet;

namespace DgraphDotNet.Transactions {

    internal class TransactionWithMutations : Transaction<DgraphRequestClient>, ITransactionWithMutations {

        internal TransactionWithMutations(DgraphRequestClient client) : base(client) {

        }

        public IMutation NewMutation() {
            AssertNotDisposed();

            return new Mutation(this);
        }

    }
}