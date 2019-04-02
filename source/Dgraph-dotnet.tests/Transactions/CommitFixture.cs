using System.Linq;
using System.Threading.Tasks;
using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class CommitFixture : TransactionFixtureBase {

        [Test]
        public async Task Commit_SetsTransactionStateToCommitted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        [Test]
        public async Task Commit_ClientNotReceiveCommitIfNoMutation() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            await client.DidNotReceive().Commit(Arg.Any<TxnContext>());
        }

        [Test]
        public async Task Commit_ClientReceivedCommitIfMutation() {
            (var client, _) = MinimalClientForMutation();

            var txn = new Transaction(client);

            await txn.Mutate("{ }");
            await txn.Commit();

            await client.Received().Commit(Arg.Any<TxnContext>());
        }

        [Test]
        public async Task Commit_FailsOnException() {
            (var client, _) = MinimalClientForMutation();
            client
                .When(fake => fake.Commit(Arg.Any<TxnContext>()))
                .Do(call => { throw new RpcException(new Status(), "Something failed"); });
            var txn = new Transaction(client);

            await txn.Mutate("{ }");
            var result = await txn.Commit();

            result.IsFailed.Should().BeTrue();
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();

            // see note in Transaction.Commit()
            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

    }
}