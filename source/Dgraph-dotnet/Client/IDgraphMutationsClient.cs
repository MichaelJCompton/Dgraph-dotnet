using System.Collections.Generic;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;

namespace DgraphDotNet {

    /// <summary>
    /// This type of client can do transactions with query and JSON mutations
    /// and build individual edges that can be added to a <see
    /// cref="IMutation"/>  and submitted as part of the transaction.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Thrown if the client
    /// has been disposed and calls are made.</exception>
    public interface IDgraphMutationsClient : IDgraphClient {

        /// <summary>
        /// Returns a new (blank/unlabelled) node.
        /// </summary>
        FluentResults.Result<IBlankNode> NewNode();

        /// <summary>
        /// Returns a node that can be identified client side by the given name.
        /// If the name is already in use, the corresponding node is returned,
        /// otherwise a new node is allocated. The name is for easy reference
        /// client side and is not persisted in the store.
        /// </summary>
        /// <remarks>Fails if <paramref name="name"/> is null or "". Can fail if
        /// zero connection fails - In that case the result is Failed. Calling
        /// this function causes Dgraph to allocate a UID for the node (though
        /// the node isn't stored until it's added to an edge in a request).
        /// </remarks> 
        FluentResults.Result<INamedNode> GetOrCreateNode(string name);

        bool IsNodeName(string name);
    }
}