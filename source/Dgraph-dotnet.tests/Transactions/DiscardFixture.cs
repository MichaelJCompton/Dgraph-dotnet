using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class DiscardFixture : TransactionFixtureBase {

        [Test]
        public void Discard_SetsTransactionStateToAborted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Discard();

            txn.TransactionState.Should().Be(TransactionState.Aborted);
        }

        [Test]
        public void Discard_ClientNotReceiveDiscardIfNoMutation() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Discard();

            client.DidNotReceive().Discard(Arg.Any<TxnContext>());
        }

        [Test]
        public void Discard_ClientReceivedDiscardIfMutation() {
            (var client, _) = MinimalClientForMutation();

            var txn = new Transaction(client);

            txn.Mutate("{ }");
            txn.Discard();

            client.Received().Discard(Arg.Is<TxnContext>(ctx => ctx.Aborted));
        }

    }
}