using System.Collections.Generic;
using System.Linq;
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
        public void Mutate_EmptyMutationDoesNothing() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            txn.Mutate(new Api.Mutation());

            client.DidNotReceive().Mutate(Arg.Any<Api.Mutation>());
            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public void Mutate_CommitNowChangesStateToCommitted() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.CommitNow = true;
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");

            txn.Mutate(mut);

            txn.TransactionState.Should().Be(TransactionState.Committed);
        }

        [Test]
        public void Mutate_PassesOnMutation() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");

            txn.Mutate(mut);

            client.Received().Mutate(Arg.Is<Api.Mutation>(m => m == mut));
        }

        [Test]
        public void Mutate_OnlyHasSetJson() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            txn.Mutate("{ }");

            client.Received().Mutate(Arg.Is<Api.Mutation>(
                mutation => mutation.Del.Count == 0
                && mutation.DeleteJson.Length == 0
                && mutation.Set.Count == 0
                && mutation.SetJson.Equals(Google.Protobuf.ByteString.CopyFromUtf8("{ }"))));
        }

        [Test]
        public void Delete_OnlyHasDeleteJson() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);

            txn.Delete("{ }");

            client.Received().Mutate(Arg.Is<Api.Mutation>(
                mutation => mutation.Del.Count == 0
                && mutation.DeleteJson.Equals(Google.Protobuf.ByteString.CopyFromUtf8("{ }"))
                && mutation.Set.Count == 0
                && mutation.SetJson.Length == 0));
        }

        [Test]
        public void Mutate_FailsOnException() {
            (var client, _) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var mut = new Api.Mutation();
            mut.SetJson = Google.Protobuf.ByteString.CopyFromUtf8("{ }");
            client.Mutate(Arg.Any<Api.Mutation>()).Throws(new RpcException(new Status(), "Something failed"));

            var result = txn.Mutate(mut);

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public void Mutate_PassesBackResult() {
            (var client, var assigned) = MinimalClientForMutation();
            var txn = new Transaction(client);
            var uids = new Dictionary<string, string> {
                { "node1", "0x1" },
                { "node2", "0x2" }
            };
            assigned.Uids.Add(uids);
            
            var result = txn.Mutate("{ }");
            
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(uids);
        }

    }
}