using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Api;
using FluentResults; // Generated library from Dgraph protos files (see project DgraphgRPC)
using Pb; // Dgraph inernal libarary for calls to zero

using System.Collections.Concurrent;
using System.Threading;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using Grpc.Core;

/* 
 * service Zero {
 * 	...
 *	rpc AssignUids (Num)               returns (api.AssignedIds) {}
 *  ...
 * }
 */

namespace DgraphDotNet {

	internal class DgraphMutationsClient : DgraphClient, IDgraphMutationsClient {

		// UIDs are allocated by Dgraph with a gRPC call to AssignUids.
		// AssignUids returns a range of UIDs allocated to this client.
		// At that point we assign uidCurrent to the low value in the range
		// and uidMaxAllocated to the top value.
		//
		// uidCurrent records the UID we would allocate next.
		// uidMaxAllocated last UID I can allocate before calling AssignUids again
		//
		private ulong uidCurrent = 1; // start > uidMaxAllocated so we call AssignUids on first request
		private ulong uidMaxAllocated; // = 0;

		private long blanksAllocated = 0;
		private readonly string blankPrefix = "_:DgraphDotNetBlank";

		private readonly ConcurrentDictionary<string, INode> nodeLookup = new ConcurrentDictionary<string, INode>();

		private string zeroAddr;

		Channel zeroChannel;
		Zero.ZeroClient zeroClient;

		internal DgraphMutationsClient(IGRPCConnectionFactory connectionFactory, ITransactionFactory transactionFactory) : base(connectionFactory, transactionFactory) {

		}

		/// <summary>
		/// A Dgraph zero needs to be connected otherwise the client can't do the managment
		/// functions needed.  
		/// </summary>
		public void ConnectZero(string address) {
			zeroAddr = address;
		}

		public ITransactionWithMutations NewTransactionWithMutations() {
            AssertNotDisposed();

            return transactionFactory.NewTransaction(this);
        }

		// 
		// ------------------------------------------------------
		//                      Nodes 
		// ------------------------------------------------------
		//
		#region Nodes

		// The concurrent dictionary is safe to read by multiple threads and has 
		// methods to attomically update.  A DgraphClient still requires a mutex
		// though because GetOrAdd(TKey, Func<TKey,TValue>) isn't executed with 
		// a lock.  From the docs it seems like it would do a bit like what's in
		// GetOrCreateNode() below, but would execute the Func outside the lock.
		// That won't work for me because I only want to execute the Func if we
		// really do need to add a new node, otherwise we'll waste node allocations
		// from the server.
		private ConcurrentDictionary<string, INamedNode> knownNodes = new ConcurrentDictionary<string, INamedNode>();

		private readonly System.Object nodeMutex = new System.Object();
		protected System.Object ThisClientNodeMutex => nodeMutex;

		public FluentResults.Result<IBlankNode> NewNode() {
			AssertNotDisposed();

			return Results.Ok<IBlankNode>(new BlankNode(blankPrefix + Interlocked.Increment(ref blanksAllocated)));
		}

		public FluentResults.Result<INamedNode> GetOrCreateNode(string name) {
			AssertNotDisposed();

			if (string.IsNullOrEmpty(name)) {
				return Results.Fail<INamedNode>(new BadArgs("Empty args"));
			}

			return NodeFromUIDOption(NextUID(), uid => GetOrCreateNode(name, knownNodes, () =>(INamedNode) new NamedNode(uid, name)));
		}

		public bool IsNodeName(string name) => knownNodes.ContainsKey(name);

		/// <summary>
		/// (thread-safe) Lookup a node name and creates only if not existing.
		/// Takes a function to create a node if the node doesn't exist.  All
		/// node creation that uses a name lookup should come through here.
		/// </summary>
		/// <remarks>Pre : <c>!string.IsNullOrEmpty(nodeName)</c> and <c>creator != null</c> and not disposed</remarks>
		/// <remarks>Post : <c>knownNodes.TryGetValue(nodeName, Result) == true</c>          
		/// and result is the new or existing node.</remarks>
		/// <returns>The existing or created node.</returns>
		/// <param name="nodeName">Node name.</param>
		/// <param name="nodeCreator">Node creator function if node doesn't exist --- 
		/// This is assumed to always succeed, so must be post UID allocation.</param>
		private TNode GetOrCreateNode<TNode>(string nodeName, IDictionary<string, TNode> nodes, Func<TNode> nodeCreator)
		where TNode : INode {
			Debug.Assert(!string.IsNullOrEmpty(nodeName) && nodeCreator != null);

			// Try reading.
			if (!nodes.TryGetValue(nodeName, out TNode resultNode)) {
				// If we couldn't get it, lock, but something might have written
				// in between.  Only execute the creation function if we must.
				lock(ThisClientNodeMutex) {
					if (!nodes.TryGetValue(nodeName, out resultNode)) {
						resultNode = nodeCreator();
						nodes[nodeName] = resultNode;
					}
				}
			}

			return resultNode;
		}

		private FluentResults.Result<TNode> NodeFromUIDOption<TNode>(FluentResults.Result<ulong> uidResult, Func<ulong, TNode> nodeCreator)
		where TNode : INode {
			if (uidResult.IsSuccess) {
				return Results.Ok<TNode>(nodeCreator(uidResult.Value));
			}

			return Results.Merge<TNode>(uidResult);
		}

		/// <summary>
		/// Allocate the next UID from this client's range allocated by the backend.
		/// If there are no more, this will go back to the server to ask for another range.
		/// </summary>
		private FluentResults.Result<ulong> NextUID() {
			if (uidCurrent <= uidMaxAllocated) {
				return FluentResults.Results.Ok<ulong>(uidCurrent++);
			}

			// dial the known zero and allocate a new range

			// the dgraph go code seems to dial, hold a connection and then mint
			// up a new connection if the last fails.  For the moment
			// I'll probably just have the one zero .. but it should be done
			// better than this.
			//
			// FIXME:  but that ^^ means redoing the Connections
			try {
				if (zeroChannel == null || zeroClient == null) {
					zeroChannel = new Channel(zeroAddr, ChannelCredentials.Insecure);
					zeroClient = new Zero.ZeroClient(zeroChannel);
				}
				var assigned = zeroClient.AssignUids(new Pb.Num() { Val = 1000 });
				uidCurrent = assigned.StartId;
				uidMaxAllocated = assigned.EndId;
				return FluentResults.Results.Ok<ulong>(uidCurrent++);
			} catch (RpcException rpcEx) {
				return FluentResults.Results.Fail<ulong>(new FluentResults.ExceptionalError(rpcEx));
			}
		}

		#endregion


		// 
		// ------------------------------------------------------
		//              disposable pattern.
		// ------------------------------------------------------
		//

		#region disposable pattern

		protected override void DisposeIDisposables() {
			if (!Disposed) {
				zeroChannel?.ShutdownAsync();
				base.DisposeIDisposables();
			}
		}

		#endregion
	}
}