using System.Collections.Generic;
using System.Threading.Tasks;
using DgraphDotNet.Graph;

namespace DgraphDotNet {

	/// <summary>
	/// This type of client can also internally mange batches of mutations. Just
	/// throw the edges in and the client manages all the Dgraph mutations and
	/// communications.  If something fails, the failed edges can be retrieved
	/// and retried.
	/// </summary>
	/// <exception cref="System.ObjectDisposedException">Thrown if the client
	/// has been disposed and calls are made.</exception>
	public interface IDgraphBatchingClient : IDgraphMutationsClient {

		/// <summary>
		/// Add the edge to a pending batch.  
		/// </summary>
		/// <remarks>Has no effect if edge is null</remarks>
		Task BatchAddEdge(Edge edge);

		/// <summary>
		/// Add the property to a pending batch.  
		/// </summary>
		/// <remarks>Has no effect if property is null</remarks>
		Task BatchAddProperty(Property property);

		/// <summary>
		/// Schedule an edge to be deleted by a pending batch.  
		/// </summary>
		/// <remarks>Has no effect if edge is null</remarks>
		Task BatchDeleteEdge(Edge edge);

		/// <summary>
		/// Schedule a property to be deleted by a pending batch.  
		/// </summary>
		/// <remarks>Has no effect if property is null</remarks>
		Task BatchDeleteProperty(Property property);

		/// <summary>
		/// Have any of the batched updates this client has submitted failed.
		/// </summary>
		bool HasFailedBatches { get; }

		/// <summary>
		/// If there have been any errors submitting batches, then the edges
		/// and properties from those failures can be retrived and tried again.
		/// The output is 
		/// ((AddedEdges, AddedPropeties), (DeletedEdges, DeletedProperties))
		/// </summary>
		((List<Edge>, List<Property>), (List<Edge>, List<Property>)) AllLinksFromFailedMutations();

		/// <summary>
		/// Flushs all pending batches.  This call assures that all batches
		/// have been submitted and are empty.  BUT there's no guarantee, by
		/// the next line of user code that some thread hasn't added to batches, 
		/// unless this is assured externally.
		/// </summary>
		Task FlushBatches();
	}
}