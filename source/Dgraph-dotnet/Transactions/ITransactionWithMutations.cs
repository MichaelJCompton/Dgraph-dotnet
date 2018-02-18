namespace DgraphDotNet.Transactions
{
    public interface ITransactionWithMutations : ITransaction
    {
        IMutation NewMutation();
    }
}