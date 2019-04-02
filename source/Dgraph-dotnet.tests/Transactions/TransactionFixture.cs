using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class TransactionFixture : TransactionFixtureBase {

        // 
        // ------------------------------------------------------
        //                   ITransaction
        // ------------------------------------------------------
        //

        // See the other test files.

        // Anything else generic to go in here ?

        // 
        // ------------------------------------------------------
        //                   All - Errors
        // ------------------------------------------------------
        //

        [Test]
        public async Task All_FailIfAlreadyCommitted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Commit();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public async Task All_FailIfDiscarded() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            await txn.Discard();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public void All_ExceptionIfDisposed() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Dispose();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                test.Should().Throw<ObjectDisposedException>();
            }
        }

        [Test]
        public async Task All_FailIfTransactionError() {
            // force transaction into error state
            var client = Substitute.For<IDgraphClientInternal>();
            client.Mutate(Arg.Any<Api.Mutation>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);
            
            await txn.Mutate("{ }");

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = await test();
                result.IsFailed.Should().BeTrue();
            }
        }

        private List<Func<Task<ResultBase>>> GetAllTestFunctions(Transaction txn) =>
            new List<Func<Task<ResultBase>>> {
                async () => await txn.Commit(),
                async () => await txn.Delete(""),
                async () => await txn.Mutate(""),
                async () => await txn.Query(""),
                async () => await txn.QueryWithVars("", null),
                async () => await txn.SchemaQuery(),
                async () => await txn.SchemaQuery("")
            };
    }
}