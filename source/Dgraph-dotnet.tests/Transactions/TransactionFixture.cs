using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json;
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

        #region Mutate

        #endregion

        #region Delete

        #endregion

        #region Discard

        #endregion

        #region Commit

        #endregion

        // 
        // ------------------------------------------------------
        //                   Other
        // ------------------------------------------------------
        //

        [Test]
        public void All_FailIfAlreadyCommitted() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Commit();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = test();
                result.IsFailed.Should().BeTrue();
            }
        }

        [Test]
        public void All_FailIfDiscarded() {
            var client = Substitute.For<IDgraphClientInternal>();
            var txn = new Transaction(client);
            txn.Discard();

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = test();
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
        public void All_FailIfTransactionError() {
            // force transaction into error state
            var client = Substitute.For<IDgraphClientInternal>();
            client.Mutate(Arg.Any<Api.Mutation>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);
            
            txn.Mutate("{ }");

            var tests = GetAllTestFunctions(txn);

            foreach (var test in tests) {
                var result = test();
                result.IsFailed.Should().BeTrue();
            }
        }

        private List<Func<ResultBase>> GetAllTestFunctions(Transaction txn) =>
            new List<Func<ResultBase>> {
                () => txn.Commit(),
                () => txn.Delete(""),
                () => txn.Mutate(""),
                () => txn.Query(""),
                () => txn.QueryWithVars("", null),
                () => txn.SchemaQuery(),
                () => txn.SchemaQuery("")
            };
    }
}