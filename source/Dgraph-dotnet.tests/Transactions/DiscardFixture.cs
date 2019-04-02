using System;
using System.Threading.Tasks;
using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class DiscardFixture : TransactionFixtureBase {

        [Test]
        public async Task Discard_SetsTransactionStateToAborted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Discard();

            txn.TransactionState.Should().Be(TransactionState.Aborted);
        }

        [Test]
        public async Task Discard_ClientNotReceiveDiscardIfNoMutation() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Discard();

            await client.DidNotReceive().Discard(Arg.Any<TxnContext>());
        }

        [Test]
        public async Task Discard_ClientReceivedDiscardIfMutation() {
            (var client, _) = MinimalClientForMutation();

            var txn = new Transaction(client);

            await txn.Mutate("{ }");
            await txn.Discard();

            await client.Received().Discard(Arg.Is<TxnContext>(ctx => ctx.Aborted));
        }

        [Test]
        public async Task Discard_DoesntFail() {
            (var client, _) = MinimalClientForMutation();
            client
                .When(fake => fake.Discard(Arg.Any<TxnContext>()))
                .Do(call => { throw new RpcException(new Status(), "Something failed"); });
            var txn = new Transaction(client);

            await txn.Mutate("{ }");
            Func<Task> test = async () => await txn.Discard();

            test.Should().NotThrow();
        }

    }
}