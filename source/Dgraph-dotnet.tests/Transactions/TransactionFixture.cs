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
    public class TransactionFixture {

        // 
        // ------------------------------------------------------
        //                   IQuery
        // ------------------------------------------------------
        //

        #region SchemaQuery

        private(IDgraphClientInternal, Response) MinimalClientForQuery() {
            var client = Substitute.For<IDgraphClientInternal>();
            var linRead = new LinRead();
            client.GetLinRead().Returns(linRead);

            var response = new Response();
            response.Txn = new TxnContext();;
            client.Query(Arg.Any<Request>()).Returns(response);

            return (client, response);
        }

        [Test]
        public void SchemaQuery_ReturnsSchema() {
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

            var result = txn.SchemaQuery("some valid query");

            result.IsSuccess.Should().Be(true);

            result.Value.Schema.Count.Should().Be(3);

        }

        [Test]
        public void SchemaQuery_PassesOnQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = @"
schema(pred: [name, friend]) {
    type
    index
}";

            var result = txn.SchemaQuery(theQuery);

            client.Received().Query(Arg.Is<Request>(r => r.Query.Equals(theQuery)));
        }

        [Test]
        public void SchemaQuery_MintsEmptySchemaQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var result = txn.SchemaQuery();

            client.Received().Query(Arg.Is<Request>(r => r.Query.Equals("schema { }")));
        }

        [Test]
        public void SchemaQuery_FailsIfQueryFails() {
            var client = Substitute.For<IDgraphClientInternal>();
            client.Query(Arg.Any<Request>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);

            var result = txn.SchemaQuery("throw");

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        [Test]
        public void SchemaQuery_FailsIfNotASchemaQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{ \"aquery\" : [ \"apred\" : 10 ] }"));
            var txn = new Transaction(client);

            // The way I'd detect that;s it's not a schema query is cause I can't parse the answer.
            var result = txn.SchemaQuery("something else");

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<JsonReaderException>();
        }

        #endregion

        #region Query

        [Test]
        public void Query_PassesOnQuery() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = "The really important query";

            var result = txn.Query(theQuery);

            client.Received().Query(Arg.Is<Request>(r => r.Query.Equals(theQuery)));
        }

        [Test]
        public void Query_PassesOnQueryAndVariables() {
            (var client, var response) = MinimalClientForQuery();
            response.Json = ByteString.CopyFrom(Encoding.UTF8.GetBytes("{  }"));
            var txn = new Transaction(client);

            var theQuery = "The really important query";
            var theVars = new Dictionary<string, string> { { "var", "val" } };

            var result = txn.QueryWithVars(theQuery, theVars);

            client.Received().Query(Arg.Is<Request>(r =>
                r.Query.Equals(theQuery)
                && r.Vars.Count == 1
                && r.Vars.First().Key.Equals("var")
                && r.Vars.First().Value.Equals("val")));
        }

        [Test]
        public void Query_FailsIfError() {
            var client = Substitute.For<IDgraphClientInternal>();
            client.Query(Arg.Any<Request>()).Throws(new RpcException(new Status(), "Something failed"));
            var txn = new Transaction(client);

            var result = txn.Query("throw");

            result.IsFailed.Should().Be(true);
            result.Errors.First().Should().BeOfType<ExceptionalError>();
            (result.Errors.First() as ExceptionalError).Exception.Should().BeOfType<RpcException>();
        }

        #endregion

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

        public void All_ExceptionIfDiscarded() {
            // grab everything from the interfaces
            // discard the transaction
            // loop through, should get an exception for each
        }
    }
}