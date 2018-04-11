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
		private readonly List<Mutex> batchesMutexes = new List<Mutex>();

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
					batchesMutexes.Add(new Mutex());
				}
			}
		}

		public void BatchAddEdge(Edge edge) {
			AssertNotDisposed();

			if (edge != null) {
				// int is atomic and it's ok if we get multiples seeing the same values.
				Task t = Task.Factory.StartNew(() => BatchUpdate(addToBatch++, (IMutation req) => { req.AddEdge(edge); }));
			}
		}

		public void BatchAddProperty(Property property) {
			AssertNotDisposed();

			if (property != null) {
				Task t = Task.Factory.StartNew(() => BatchUpdate(addToBatch++, (IMutation req) => { req.AddProperty(property); }));
			}
		}

		public void BatchDeleteEdge(Edge edge) {
			AssertNotDisposed();

			if (edge != null) {
				Task t = Task.Factory.StartNew(() => BatchUpdate(addToBatch++, (IMutation req) => { req.DeleteEdge(edge); }));
			}
		}

		public void BatchDeleteProperty(Property property) {
			AssertNotDisposed();

			if (property != null) {
				Task t = Task.Factory.StartNew(() => BatchUpdate(addToBatch++, (IMutation req) => { req.DeleteProperty(property); }));
			}
		}

		private void BatchUpdate(int batch, Action<IMutation> updateFN) {
			batchesMutexes[batch].WaitOne();
			try {
				updateFN(batches[batch]);
				if (batches[batch].NumAdditions + batches[batch].NumDeletions >= batchSize) {
					SubmittBatch(batch);
					batches[batch] = new Mutation();
				}
			} finally {
				batchesMutexes[batch].ReleaseMutex();
			}
		}

		// must hold the batch mutex to call this
		private void SubmittBatch(int batch) {
			using(var txn = NewTransactionWithMutations()) {
				var err = batches[batch].SubmitTo(txn);
				if(err.IsFailed) {
					FailBatch(batches[batch]);
				}
			}
		}

		public void FlushBatches() {
			for (int i = 0; i < NumBatches; i++) {
				batchesMutexes[i].WaitOne();
				SubmittBatch(i);
			}

			// At this point all the batches are empty
			// and all the locks are held here.

			for (int i = 0; i < NumBatches; i++) {
				batchesMutexes[i].ReleaseMutex();
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