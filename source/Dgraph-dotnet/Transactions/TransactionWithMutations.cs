using System.Collections.Generic;
using System.Threading.Tasks;
using DgraphDotNet;

namespace DgraphDotNet.Transactions {

    internal class TransactionWithMutations : Transaction, ITransactionWithMutations {

        internal TransactionWithMutations(DgraphMutationsClient client) : base(client) {

        }

        public IMutation NewMutation() {
            AssertNotDisposed();

            return new Mutation();
        }

        public async Task<FluentResults.Result<IDictionary<string, string>>> ApiMutate(Api.Mutation mutation) {
            AssertNotDisposed();
            
            return await base.Mutate(mutation);
        }

    }
}