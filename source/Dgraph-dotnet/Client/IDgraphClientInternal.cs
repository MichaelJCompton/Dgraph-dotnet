using System.Threading.Tasks;
using Api;

namespace DgraphDotNet {

    /// <summary>
    /// Internal dealings of clients with Dgraph --- Not part of the
    /// external interface
    /// </summary>
    internal interface IDgraphClientInternal {

        Task<Response> Query(Request req);

        Task<Assigned> Mutate(Api.Mutation mut);

        Task Commit(TxnContext context);

        Task Discard(TxnContext context);
        
    }
}