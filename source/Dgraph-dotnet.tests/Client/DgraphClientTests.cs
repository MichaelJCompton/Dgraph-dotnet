using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DgraphDotNet;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using FluentResults;
using NSubstitute;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Client {
    public class DgraphClientTests {
        DgraphClient client;
        ITransactionFactory transactionFactory;

        [SetUp]
        public void Setup() {
            var connectionFactory = Substitute.For<IGRPCConnectionFactory>();
            transactionFactory = Substitute.For<ITransactionFactory>();
            client = new DgraphClient(connectionFactory, transactionFactory);
        }

        [Test]
        public async Task UpsertAllocatesNewNode() {
            var txn = Substitute.For<ITransaction>();
            transactionFactory.NewTransaction(client).Returns(txn);

            txn.Query(Arg.Any<string>()).Returns(Results.Ok<string>("{\"q\":[]}"));
            txn.Mutate(Arg.Any<string>()).Returns(Results.Ok<IDictionary<string, string>>(new Dictionary<string, string> { { "aBlankName", "0x1bf" } }));
            txn.Commit().Returns(Results.Ok());

            var result = await client.Upsert(
                "aPredicate", 
                GraphValue.BuildStringValue("aString"), 
                "a mutation",
                "aBlankName");

            Assert.IsTrue(result.IsSuccess);
            var (node,existed) = result.Value;
            Assert.IsFalse(existed);
            switch (node) {
                case UIDNode uidnode:
                    Assert.AreEqual((ulong) 447, uidnode.UID);
                    break;
                default:
                    Assert.Fail("Wrong node type");
                    break;
            }
        }

        [Test]
        public async Task UpsertReturnsExistingNode() { 
            var txn = Substitute.For<ITransaction>();
            transactionFactory.NewTransaction(client).Returns(txn);

            txn.Query(Arg.Any<string>()).Returns(Results.Ok<string>("{\"q\":[{\"uid\":\"0x1bf\"}]}"));

            var result = await client.Upsert(
                "aPredicate", 
                GraphValue.BuildStringValue("aString"), 
                "a mutation",
                "aBlankName");

            Assert.IsTrue(result.IsSuccess);
            var (node,existed) = result.Value;
            Assert.IsTrue(existed);
            switch (node) {
                case UIDNode uidnode:
                    Assert.AreEqual((ulong) 447, uidnode.UID);
                    break;
                default:
                    Assert.Fail("Wrong node type");
                    break;
            }
        }

        [Test]
        public async Task UpsertHandlesTransactionConflict() { 
            var txn = Substitute.For<ITransaction>();
            transactionFactory.NewTransaction(client).Returns(txn);

            txn.Query(Arg.Any<string>()).Returns(Results.Ok<string>("{\"q\":[]}"), Results.Ok<string>("{\"q\":[{\"uid\":\"0x1bf\"}]}"));
            txn.Mutate(Arg.Any<string>()).Returns(Results.Ok<IDictionary<string, string>>(new Dictionary<string, string> { { "aBlankName", "0xfff" } }));
            txn.Commit().Returns(Results.Fail("This transaction had a conflict"));

            var result = await client.Upsert(
                "aPredicate", 
                GraphValue.BuildStringValue("aString"), 
                "a mutation",
                "aBlankName");

            Assert.IsTrue(result.IsSuccess);
            var (node,existed) = result.Value;
            Assert.IsTrue(existed);
            switch (node) {
                case UIDNode uidnode:
                    Assert.AreEqual((ulong) 447, uidnode.UID);
                    break;
                default:
                    Assert.Fail("Wrong node type");
                    break;
            }
        }

        [Test]
        public async Task UpsertReturnsError() { 
            var txn = Substitute.For<ITransaction>();
            transactionFactory.NewTransaction(client).Returns(txn);

            txn.Query(Arg.Any<string>()).Returns(Results.Fail<string>("This didn't work"));

            var node = await client.Upsert(
                "aPredicate", 
                GraphValue.BuildStringValue("aString"), 
                "a mutation",
                "aBlankName");

            Assert.IsTrue(node.IsFailed);
        }

    }
}