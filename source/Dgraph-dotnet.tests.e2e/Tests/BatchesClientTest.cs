using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Orchestration;

namespace Dgraph_dotnet.tests.e2e.Tests {
    public class BatchesClientTest : GraphSchemaE2ETest {
        public BatchesClientTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        // Can't happen till I extend the clients to allow secure connections to zero

        public async override Task Test() {
            // Test1
            // Test2
            // Test3

            await Task.Run(() => {});
        }

    }
}