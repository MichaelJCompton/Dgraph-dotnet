using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DgraphDotNet.Graph {
	/// <summary>
	/// Dgraph assigns a UID to all nodes.  But nodes minted client side might not
	/// yet have a UID.  Nodes minted by the client can reserve a UID on
	/// creation (even before being persisted in the store) and be stored client-side
	/// in a name -> UID map.  
	/// </summary>
	public interface INode : IEdgeTarget {

	}

	/// <summary>
	/// A blank node is just a node. Allows to link with edges for a single mutation.  
	/// If the same blank node is used in multiple mutations it ends will end up as
	/// different nodes in the graph.
	/// </summary>
	public interface IBlankNode : INode {

		/// <summary>
		/// Why does a blank node have a name?  Because Dgraph returns a map
		/// of nodes it allocated in a mutation.  This name can be used to look up
		/// that list and get the UID of the Dgraph UID allocated to the node.
		/// </summary>
		string BlankNodeName { get; }
	}

	/// <summary>
	/// A node that can be referenced client-side by it's Name and has a known UID.
	/// These nodes can be looked up in the client by name, but the name is not
	/// persisted in the store.  When persisted in the store it will have the 
	/// given UID, so it can be used across multiple mutations and multiple
	/// transactions.  If the node never gets persisted the UID is not recycled.
	/// </summary>
	public interface INamedNode : INode {

		/// <summary>
		/// The UID of this node. 
		/// If the node has not yet been added to a Dgraph store, the UID is the value 
		/// the node will have when added.  If it's a node already in the store,
		/// it's the UID of the node in the store.</summary>
		ulong UID { get; }

		string Name { get; }
	}

	/// <summary>
	/// A node that can be referenced client side by it's XID and where the
	/// XID is saved in the store with a property "xid" for the node.
	/// </summary>
	public interface IXIDNode : INamedNode {

		/// <summary>
		/// XID == Name
		/// </summary>
		/// <returns></returns>
		string XID { get; }
	}

	internal class BlankNode : IBlankNode {
		public string BlankNodeName { get; protected set; }

		internal BlankNode(string blankId) {
			BlankNodeName = blankId;
		}
	}


	internal class NamedNode : INamedNode {
		public string Name { get; private set; }

		public ulong UID { get; private set; }

		internal NamedNode(ulong UID, string name) {
			Debug.Assert(!string.IsNullOrEmpty(name));

			this.UID = UID; 
			this.Name = name;
		}
	}

	internal class XIDNode : NamedNode, IXIDNode {
		public bool XIDAdded { get; private set; }
		public void XIDWasAdded() => XIDAdded = true;

		public string XID => Name;

		internal XIDNode(ulong UID, string XID) : base(UID, XID) { }
	}
}