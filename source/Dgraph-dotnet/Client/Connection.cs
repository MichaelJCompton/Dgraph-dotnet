using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Api;
using Grpc.Core;

namespace DgraphDotNet {

	internal interface IGRPCConnection : IDisposable {
		string Target { get; }
		Task Alter(Api.Operation op);
        Task<Api.Version> CheckVersion();
		Task<Response> Query(Api.Request req);
		Task<Response> Mutate(Api.Request mut);
		Task Commit(TxnContext context);
		Task Discard(TxnContext context);
	}

	/// <summary>
	/// A gRPC connection wrapping a <c>Protos.Dgraph.DgraphClient</c>.  
	/// Doesn't check for rpcExceptions or other failures --- it's the job of the calling
	/// classes to know what to do if a connection is faulty.
	/// </summary>
	internal class GRPCConnection : IGRPCConnection {

		private readonly Api.Dgraph.DgraphClient connection;

		// grpc chans have public ChannelState State { get; }
		// could use this to expose a state if needed
		// https://grpc.io/grpc/csharp/api/Grpc.Core.ChannelState.html
		private readonly Channel channel;

		/// <remarks>
		///       Pre : <c>channel != null</c> <c>connection != null</c> and 
		///       this is the channel used to make the connection. 
		/// </remarks>
		public GRPCConnection(Channel channel, Api.Dgraph.DgraphClient connection) {
			Debug.Assert(channel != null);
			Debug.Assert(connection != null);

			this.channel = channel;
			this.connection = connection;
		}

		public string Target => channel.Target;

		// FIXME: should allow cancellation tokens, deadlines, etc??

		#region mutations

		public async Task Alter(Api.Operation op) {
			AssertNotDisposed();

			await connection.AlterAsync(op);
		}

		public async Task<Api.Version> CheckVersion() {
			AssertNotDisposed();

			return await connection.CheckVersionAsync(new Check());
		}

		public async Task<Response> Query(Api.Request req) {
			AssertNotDisposed();

			return await connection.QueryAsync(req);
		}

		public async Task<Response> Mutate(Api.Request mut) {
			AssertNotDisposed();

			return await connection.QueryAsync(mut);
		}

		public async Task Commit(TxnContext context) {
			AssertNotDisposed();

			await connection.CommitOrAbortAsync(context);
		}

		public async Task Discard(TxnContext context) {
			AssertNotDisposed();

			await connection.CommitOrAbortAsync(context);
		}

		#endregion

		// 
		// ------------------------------------------------------
		//              disposable pattern.
		// ------------------------------------------------------
		//
		#region disposable pattern

		// From the docs : https://grpc.io/grpc/csharp/api/Grpc.Core.Channel.html
		//
		// "It is strongly recommended to shutdown all previously created channels before exiting from the process."
		//
		// I could treat this like an unmanaged resource, but that would mean using a finalizer and should I really
		// be calling async code in there, setting up new tasks and doing network ops? And what happends when all 
		// that's called during program exit?  
		// Seems best to treat as a managed resource and just expect the user to call Dispose(). 

		private bool disposed; // = false;

		protected void AssertNotDisposed() {
			if (this.disposed) {
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		public void Dispose() {
			if (!this.disposed) {
				// returns a Task, but ignoring
				channel.ShutdownAsync(); // exceptions ???
			}
			this.disposed = true;
		}

		#endregion
	}

}