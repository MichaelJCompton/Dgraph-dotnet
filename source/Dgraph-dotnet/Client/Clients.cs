// For unit testing.  Allows to make mocks of the internal interfaces and factories
// so can test in isolation from a Dgraph instance.
//
// When I put this in an AssemblyInfo.cs it wouldn't compile any more.
[assembly : System.Runtime.CompilerServices.InternalsVisibleTo("Dgraph-dotnet.tests")]
[assembly : System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // for NSubstitute

namespace DgraphDotNet {
    public class Clients {

        /// <summary>
        /// Create a client that can do query and JSON mutations.
        /// </summary>
        public static IDgraphClient NewDgraphClient() {
            return new DgraphClient(new GRPCConnectionFactory());
        }

        /// <summary>
        /// Create a client that can do query, JSON mutations and build individual edges into sets of
        /// mutations that are sent to the store.
        /// </summary>
        /// <returns></returns>
        public static IDgraphMutationsClient NewDgraphRequestClient(string zeroAddress) {
            var client = new DgraphRequestClient(new GRPCConnectionFactory());
            client.ConnectZero(zeroAddress);
            return client;
        }

        /// <summary>
        /// Create a client that can do query, JSON mutations, edge mutations
        /// and use batching mode - just submit edges and the client takes care of forming 
        /// the submitted edges into batches and submitting in transaction to the backend store.
        /// 
        /// By default it holds open 100 batches, places edges in batches and submits a batch when it has 100
        /// changes (sum of additions and deletions) in it.
        /// </summary>
        public static IDgraphBatchingClient NewDgraphBatchingClient(string zeroAddress) {
            var client = new DgraphBatchingClient(new GRPCConnectionFactory());
            client.ConnectZero(zeroAddress);
            return client;
        }

        public static IDgraphBatchingClient NewDgraphBatchingClient(string zeroAddress, int numBatches, int batchSize) {
            var client = new DgraphBatchingClient(new GRPCConnectionFactory(), numBatches, batchSize);
            client.ConnectZero(zeroAddress);
            return client;
        }
    }
}