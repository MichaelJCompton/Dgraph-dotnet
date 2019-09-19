using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class MutateDeleteFixture : TransactionFixtureBase
    {
        
        [Test]
        public async Task Mutate_EmptyMutationDoesNothing() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            await txn.Mutate(new Api.Mutation());

            await client.DidNotReceive().Mutate(Arg.Any<Api.Request>());
            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public async Task Mutate_CommitNowChangesStateToCommitted() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.CommitNow = true;
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");

            await txn.Mutate(mut);

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        [Test]
        public async Task Mutate_PassesOnMutation() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");

            await txn.Mutate(mut);

            await client.Received().Mutate(Arg.Is<Api.Request>(req => req.Mutations[0] == mut));
        }

        [Test]
        public async Task Mutate_OnlyHasSetJson() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            await txn.Mutate("{ }");

            await client.Received().Mutate(Arg.Is<Api.Request>(
                req => req.Mutations[0].Del.Count == 0
                && req.Mutations[0].DeleteJson.Length == 0
                && req.Mutations[0].Set.Count == 0
                && req.Mutations[0].SetJson.Equals(Google.Protobuf.ByteString.CopyFromUtf8("{ }"))));
        }

        [Test]
        public async Task Delete_OnlyHasDeleteJson() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            await txn.Delete("{ }");

            await client.Received().Mutate(Arg.Is<Api.Request>(
                req => req.Mutations[0].Del.Count == 0
                && req.Mutations[0].DeleteJson.Equals(Google.Protobuf.ByteString.CopyFromUtf8("{ }"))
                && req.Mutations[0].Set.Count == 0
                && req.Mutations[0].SetJson.Length == 0));
        }

        [Test]
        public async Task Mutate_FailsOnException() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");
            client.Mutate(Arg.Any<Api.Request>()).Throws(new RpcException(new Status(), "Something failed"));

            var result = await txn.Mutate(mut);

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task Mutate_PassesBackResult() {
            (var client, var assigned) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var uids = new Dictionary<string, string> {
                { "node1", "0x1" },
                { "node2", "0x2" }
            };
            assigned.Uids.Add(uids);
            
            var result = await txn.Mutate("{ }");
            
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(uids);
        }

    }
}