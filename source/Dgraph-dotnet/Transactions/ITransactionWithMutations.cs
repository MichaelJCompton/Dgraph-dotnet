using System.Collections.Generic;
using System.Threading.Tasks;

namespace DgraphDotNet.Transactions
{
    public interface ITransactionWithMutations : ITransaction
    {
        IMutation NewMutation();

        Task<FluentResults.Result<IDictionary<string, string>>> ApiMutate(Api.Mutation mutation);
    }
}