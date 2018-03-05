

namespace DgraphDotNet.Transactions
{
    internal interface ITransactionFactory {
        ITransaction NewTransaction(DgraphClient client);

        ITransactionWithMutations NewTransaction(DgraphMutationsClient client);
    }

    internal class TransactionFactory : ITransactionFactory
    {
        public ITransaction NewTransaction(DgraphClient client) {
            return new Transaction(client);
        }

        public ITransactionWithMutations NewTransaction(DgraphMutationsClient client) {
            return new TransactionWithMutations(client);
        }
    }
}