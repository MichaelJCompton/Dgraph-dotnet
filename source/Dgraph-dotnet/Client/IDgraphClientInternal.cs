using Api;

namespace DgraphDotNet {

    /// <summary>
    /// Internal dealings of clients with Dgraph --- Not part of the
    /// external interface
    /// </summary>
    internal interface IDgraphClientInternal {

        Response Query(Request req);

        Assigned Mutate(Api.Mutation mut);

        void Commit(TxnContext context);

        void Discard(TxnContext context);

        LinRead GetLinRead();

        void MergeLinRead(LinRead newLinRead);
    }
}