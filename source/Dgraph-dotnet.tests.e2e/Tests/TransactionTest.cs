using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Orchestration;

namespace Dgraph_dotnet.tests.e2e.Tests {
    public class TransactionTest : GraphSchemaE2ETest {
        public TransactionTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Test() {
            // Test1
            // Test2
            // Test3
        }

        
        // This is probably the best way to test handling of transactions

        // aborted transactions have no effect

        // what happens with interleaved transactions etc
        // transaction is error

        // what's visible about an uncommited transaction

        // what can competing transactions see of each other

        // upsert ... + upsert works with expected types

        // etc...



    }
}