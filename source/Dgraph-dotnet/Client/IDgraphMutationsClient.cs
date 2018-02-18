using System.Collections.Generic;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;

/* 
 * service Zero {
 *  ...
 *  rpc AssignUids (Num) returns (api.AssignedIds) {}
 *  ...
 * }
 */

namespace DgraphDotNet {

    /// <summary>
    /// This type of client can do transactions with query and JSON mutations
    /// and build individual edges that can be added to a <see
    /// cref="IMutation"/>  and submitted as part of the transaction.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Thrown if the client
    /// has been disposed and calls are made.</exception>
    public interface IDgraphMutationsClient : IDgraphClient {

        ITransactionWithMutations NewTransactionWithMutations();

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

        // FIXME: argh, this XID stuff is rubbish!  I put it in in like when
        // dgraph was at like v0.8 and XID was a thing.  Much better now to have
        // upsert support and allow any edge and maybe have some special
        // faciliies for linked data uri.  Other than that can also cache client
        // side global identifiers to nodes in a map.

        /// <summary>
        /// Returns a node also identified by the xid server side. If the name
        /// is already in use, the corresponding node is returned, otherwise a
        /// new node is allocated. The name will be persisted in the Dgraph
        /// store with edge 'xid' when an edge involving the node is added to a
        /// request. The name is not added to the store until a request
        /// including an edge that is either sourced from or targets this node
        /// is run.      
        /// </summary>
        /// <remarks>If <paramref name="xid"/> is null or "" an unlabelled/blank
        /// node is returned (see <see cref="NewNode()"/>). Can fail if zero
        /// connection fails - In that case the result is Failed. Calling this
        /// function causes Dgraph to allocate a UID for the node (though the
        /// node isn't stored until it's added to an edge in a request).
        /// </remarks>   
        FluentResults.Result<IXIDNode> GetOrCreateXIDNode(string xid);

        bool IsXIDName(string name);

        /// <summary>
        /// Build an Edge --- the edge is not yet added to any request.
        /// </summary>
        /// <remarks>Any null or "" facets are ignored.</remarks>
        /// <remarks>Fails if <paramref name="source"/> or <paramref
        /// name="target"/> is null or <paramref name="edgeName"/> is null or ""
        /// </remarks>
        FluentResults.Result<Edge> BuildEdge(INode source, string edgeName, INode target, IDictionary<string, string> facets = null);

        /// <summary>
        /// Build a Property --- the property is not yet added to a request.
        /// </summary>
        /// <remarks>Any null or "" facets are ignored.</remarks>
        /// <remarks>Fails if <paramref name="source"/> or <paramref
        /// name="value"/> is null or <paramref name="predicateName"/> is null
        /// or ""
        /// </remarks>
        FluentResults.Result<Property> BuildProperty(INode source, string predicateName, GraphValue value, IDictionary<string, string> facets = null);
    }
}