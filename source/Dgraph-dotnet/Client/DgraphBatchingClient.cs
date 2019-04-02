using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;

namespace DgraphDotNet {

	internal class DgraphBatchingClient : DgraphMutationsClient, IDgraphBatchingClient {

		internal static int DEFAULT_BatchSize = 100;
		internal static int DEFAULT_Batches = 100;

		internal DgraphBatchingClient(IGRPCConnectionFactory connectionFactory, ITransactionFactory transactionFactory) : base(connectionFactory, transactionFactory) {
			SetBatchOptions(DEFAULT_Batches, DEFAULT_BatchSize);
		}

		internal DgraphBatchingClient(IGRPCConnectionFactory connectionFactory, ITransactionFactory transactionFactory, int numBatches, int batchSize) : base(connectionFactory, transactionFactory) {
			SetBatchOptions(numBatches, batchSize);
		}

		// 
		// ------------------------------------------------------
		//                   Batches
		// ------------------------------------------------------
		//
		#region batches

		// If we allow multiple threads then some routines will need to lock
		// the whole instance.  
		//
		// There is also ReaderWriterLockSlim which allows EnterUpgradeableReadLock()
		// followed by EnterWriteLock() and also TryEnterWriteLock(timeout).
		// If it turns out there is lots of useless read contention in some usecases,
		// that might be another way - but requres making sure all locks are released
		// on exit.
		private readonly System.Object clientMutex = new System.Object();
		protected System.Object ThisClientMutex => clientMutex;

		private readonly List<IMutation> batches = new List<IMutation>();
		private readonly List<SemaphoreSlim> batchesMutexes = new List<SemaphoreSlim>();

		protected int NumBatches => batches.Count;

		private int batchSize;
		protected int BatchSize => batchSize;

		int addToBatch = 0;

		private void SetBatchOptions(int numBatches, int batchSize) {
			AssertNotDisposed();

			lock(ThisClientMutex) {
				this.batchSize = batchSize;

				for (int i = 0; i < numBatches; i++) {
					batches.Add(new Mutation());
					batchesMutexes.Add(new SemaphoreSlim(1));
				}
			}
		}

		public async Task  BatchAddEdge(Edge edge) {
			AssertNotDisposed();

			if (edge != null) {
				await BatchUpdate((IMutation req) => { req.AddEdge(edge); });
			}
		}

		public async Task BatchAddProperty(Property property) {
			AssertNotDisposed();

			if (property != null) {
				await BatchUpdate((IMutation req) => { req.AddProperty(property); });
			}
		}

		public async Task BatchDeleteEdge(Edge edge) {
			AssertNotDisposed();

			if (edge != null) {
				await BatchUpdate((IMutation req) => { req.DeleteEdge(edge); });
			}
		}

		public async Task BatchDeleteProperty(Property property) {
			AssertNotDisposed();

			if (property != null) {
				await BatchUpdate((IMutation req) => { req.DeleteProperty(property); });
			}
		}

		private async Task BatchUpdate(Action<IMutation> updateFN) {
			// doesn't really matter if threads compete here and end up getting
			// the same batch.  We just need it to roll around.
			var batch = addToBatch;
			addToBatch = (batch  + 1) % batches.Count;
			await batchesMutexes[batch].WaitAsync();
			try {
				updateFN(batches[batch]);
				if (batches[batch].NumAdditions + batches[batch].NumDeletions >= batchSize) {
					await SubmittBatch(batch);
					batches[batch] = new Mutation();
				}
			} finally {
				batchesMutexes[batch].Release();
			}
		}

		// must hold the batch mutex to call this
		private async Task SubmittBatch(int batch) {
			using(var txn = NewTransactionWithMutations()) {
				var err = await batches[batch].SubmitTo(txn);
				if(err.IsFailed) {
					FailBatch(batches[batch]);
				}
			}
		}

		public async Task FlushBatches() {
			for (int i = 0; i < NumBatches; i++) {
				await batchesMutexes[i].WaitAsync();
				await SubmittBatch(i);
			}

			// At this point all the batches are empty
			// and all the locks are held here.

			for (int i = 0; i < NumBatches; i++) {
				batchesMutexes[i].Release();
			}

			// ... but no guarantee that some other user thread hasn't added 
			// things now.
		}

		#endregion


		// 
		// ------------------------------------------------------
		//              Failures
		// ------------------------------------------------------
		//
		#region Failures

		// Public methods on concurrent bag are thread safe, but extension
		// ones aren't, so can't use ToList() for example
		private ConcurrentBag<IMutation> failedBatches = new ConcurrentBag<IMutation>();

		public bool HasFailedBatches => !failedBatches.IsEmpty;

		public ((List<Edge>, List<Property>), (List<Edge>, List<Property>)) AllLinksFromFailedMutations() {
			List<Edge> AddEdges = new List<Edge>();
			List<Property> AddProperties = new List<Property>();
			List<Edge> DelEdges = new List<Edge>();
			List<Property> DelProperties = new List<Property>();

			while(failedBatches.TryTake(out IMutation mut)) {
				var (addE, addP) = mut.AllAddLinks();
				var (delE, delP) = mut.AllDeleteLinks();

				AddEdges.AddRange(addE);
				AddProperties.AddRange(addP);
				DelEdges.AddRange(delE);
				DelProperties.AddRange(delP);
			}

			return ((AddEdges, AddProperties), (DelEdges, DelProperties));
		}

		private void FailBatch(IMutation mut) {
			failedBatches.Add(mut);
		}

		#endregion


		// 
		// ------------------------------------------------------
		//              disposable pattern.
		// ------------------------------------------------------
		//
		#region disposable pattern.

		protected override void DisposeIDisposables() {
			if (!Disposed) {
				batchesMutexes.ForEach(mut => { mut.Dispose(); });
				base.DisposeIDisposables();
			}
		}

		#endregion

	}
}