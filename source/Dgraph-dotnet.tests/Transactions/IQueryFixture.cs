using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Google.Protobuf;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class IQueryFixture : TransactionFixtureBase {

        // 
        // ------------------------------------------------------
        //                   SchemaQuery
        // ------------------------------------------------------
        //

        #region SchemaQuery

        [Test]
        public async Task SchemaQuery_ReturnsSchema() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var queryResult = @"{ ""schema"": [
      {
        ""predicate"": ""aPredicate"",
        ""type"": ""string"",
        ""index"": true,
        ""tokenizer"": [
          ""term""
        ],
        ""upsert"": true,
        ""lang"": true
      },
      {
        ""predicate"": ""anotherPredicate"",
        ""type"": ""int"",
        ""count"": true,
        ""list"": true
      },
      {
        ""predicate"": ""alink"",
        ""type"": ""uid"",
        ""reverse"": true,
        ""count"": true
      }
    ]
  }";

            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes(queryResult));

            var result = await txn.SchemaQuery("schema { }");

            result.IsSuccess.Should().Be(true);

            result.Value.Schema.Count.Should().Be(3);

        }

        [Test]
        public async Task SchemaQuery_PassesOnQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = @"
schema(pred: [name, friend]) {
    type
    index
}";

            var result = await txn.SchemaQuery(theQuery);

            await client.Received().Query(Arg.Is<Request>(r => r.Query.Equals(theQuery)));
        }

        [Test]
        public async Task SchemaQuery_MintsEmptySchemaQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var result = await txn.SchemaQuery();

            await client.Received().Query(Arg.Is<Request>(r => r.Query.Equals("schema { }")));
        }

        [Test]
        public async Task SchemaQuery_FailsIfQueryFails() {
            var client = Substitute.For<IDgraphClientInternal>();
            client.Query(Arg.Any<Request>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);

            var result = await txn.SchemaQuery();

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task SchemaQuery_FailsIfNotASchemaQuery() {
            (var client, var response) = MinimalClientForQuery();
            var txn = new Transaction(client);

            var result = await txn.SchemaQuery("q(func: uid(0x1)) { blaa }");

            result.IsFailed.Should().Be(true);
        }

        #endregion

        // 
        // ------------------------------------------------------
        //                      Query
        // ------------------------------------------------------
        //

        #region Query

        [Test]
        public async Task Query_PassesOnQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = "The really important query";

            var result = await txn.Query(theQuery);

            await client.Received().Query(Arg.Is<Request>(r => r.Query.Equals(theQuery)));
        }

        [Test]
        public async Task Query_PassesOnQueryAndVariables() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = "The really important query";
            var theVars = new Dictionary<string, string> { { "var", "val" } };

            var result = await txn.QueryWithVars(theQuery, theVars);

            await client.Received().Query(Arg.Is<Request>(r =>
                r.Query.Equals(theQuery)
                && r.Vars.Count == 1
                && r.Vars.First().Key.Equals("var")
                && r.Vars.First().Value.Equals("val")));
        }

        [Test]
        public async Task Query_PassesBackResult() {
            (var client, var response) = MinimalClientForQuery();
            var json = "{ some-json }";
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes(json));
            var txn = new Transaction(client);

            var theQuery = "The really important query";
            var theVars = new Dictionary<string, string> { { "var", "val" } };

            var result = await txn.QueryWithVars(theQuery, theVars);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(json);
        }

        [Test]
        public async Task Query_FailsIfError() {
            var client = Substitute.For<IDgraphClientInternal>();
            client.Query(Arg.Any<Request>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);

            var result = await txn.Query("throw");

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public async Task Query_FailDoesntChangeTransactionOKState() {
            var client = Substitute.For<IDgraphClientInternal>();
            client.Query(Arg.Any<Request>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);

            await txn.Query("throw");

            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        [Test]
        public async Task Query_SuccessDoesntChangeTransactionOKState() {
            (var client, var response) = MinimalClientForQuery();
            var json = "{ some-json }";
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes(json));
            var txn = new Transaction(client);

            var theQuery = "The really important query";
            var theVars = new Dictionary<string, string> { { "var", "val" } };

            await txn.QueryWithVars(theQuery, theVars);

            txn.TransactionState.Should().Be(TransactionState.OK);
        }

        #endregion
    }
}