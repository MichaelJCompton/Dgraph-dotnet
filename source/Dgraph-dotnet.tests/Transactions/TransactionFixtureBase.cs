using Api;
using DgraphDotNet;
using NSubstitute;

namespace Dgraph_dotnet.tests.Transactions {
    public class TransactionFixtureBase {

        internal (IDgraphClientInternal, Response) MinimalClientForQuery() {
            var client = Substitute.For<IDgraphClientInternal>();
            var linRead = new LinRead();
            client.GetLinRead().Returns(linRead);

            var response = new Response();
            response.Txn = new TxnContext();;
            client.Query(Arg.Any<Request>()).Returns(response);

            return (client, response);
        }
    }
}