using System.Collections.Generic;


namespace DgraphDotNet.Transactions
{
    public interface ITransactionWithMutations : ITransaction
    {
        IMutation NewMutation();

        FluentResults.Result<IDictionary<string, string>> ApiMutate(Api.Mutation mutation);
    }
}