using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Orchestration;
using Dgraph_dotnet.tests.e2e.Tests.TestClasses;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// This is not an exhaustive test of Dgraph's transactional behaviour.  However,
// there's some client-side transaction state and handling, so this tests that
// the cient still holds the expected behaviours in the event that the
// expectations on that client-side handling changes.
//
// That client side handling did reduce in v0.6.0 of this lib (see
// d2adac9ccdd8cdde50fad69e827820d6243e44b1).

namespace Dgraph_dotnet.tests.e2e.Tests {

    public class TransactionTest : GraphSchemaE2ETest {

        public TransactionTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Setup() {
            await base.Setup();
            var alterSchemaResult = await ClientFactory.GetDgraphClient().AlterSchema(ReadEmbeddedFile("test.schema"));
            AssertResultIsSuccess(alterSchemaResult);
        }

        public async override Task Test() {
            using(var client = ClientFactory.GetDgraphClient()) {

                // mutate & query interleaving
                await NoDirtyReads(client);
                await TransactionsAreSerlializable(client);
                await DiscardedTransactionsHaveNoEffect(client);

                // mutate & mutate interleaving
                await UnrelatedTransactionsDoNotConflict(client);
                await ConflictingTransactionsDontBothSucceed(client);
            }
        }

        #region mutate-query

        private async Task NoDirtyReads(IDgraphClient client) {

            var txn1 = client.NewTransaction();
            var person = MintAPerson(nameof(NoDirtyReads));
            var json = JsonConvert.SerializeObject(person);
            var transactionResult = await txn1.Mutate(json);
            AssertResultIsSuccess(transactionResult);
            person.Uid = transactionResult.Value[person.Uid.Substring(2)];

            // Can we see this in the index?
            var queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", person.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");

            // Can we see it in the uid (note that uid queries always return the
            // uid ... we are interested if returns the actual person or not)
            var queryByUid = await client.Query(FriendQueries.QueryByUid(person.Uid));
            AssertResultIsSuccess(queryByUid);
            queryByUid.Value.Should().Be($"{{\"q\":[{{\"uid\":\"{person.Uid}\"}}]}}");

            AssertResultIsSuccess(await txn1.Commit());

            queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", person.Name } });
            AssertResultIsSuccess(queryByName);

            FriendQueries.AssertStringIsPerson(queryByName.Value, person);
        }

        private async Task TransactionsAreSerlializable(IDgraphClient client) {

            var txn1 = client.NewTransaction();
            var txn2 = client.NewTransaction();

            var person = MintAPerson(nameof(TransactionsAreSerlializable));
            var json = JsonConvert.SerializeObject(person);
            var transactionResult = await txn1.Mutate(json);
            AssertResultIsSuccess(transactionResult);
            person.Uid = transactionResult.Value[person.Uid.Substring(2)];

            // Can we see it
            var queryByName = await txn2.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", person.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");

            AssertResultIsSuccess(await txn1.Commit());

            // can we see it now - still in txn2
            queryByName = await txn2.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", person.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");

            await txn2.Discard();
        }

        private async Task DiscardedTransactionsHaveNoEffect(IDgraphClient client) {

            var txn1 = client.NewTransaction();

            var person = MintAPerson(nameof(DiscardedTransactionsHaveNoEffect));
            var json = JsonConvert.SerializeObject(person);
            var transactionResult = await txn1.Mutate(json);
            AssertResultIsSuccess(transactionResult);
            person.Uid = transactionResult.Value[person.Uid.Substring(2)];

            await txn1.Discard();

            // Can we see it
            var queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", person.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");

            // Can we see it in the uid (note that uid queries always return the
            // uid ... we are interested if returns the actual person or not)
            var queryByUid = await client.Query(FriendQueries.QueryByUid(person.Uid));
            AssertResultIsSuccess(queryByUid);
            queryByUid.Value.Should().Be($"{{\"q\":[{{\"uid\":\"{person.Uid}\"}}]}}");
        }

        #endregion

        #region mutate-mutate

        private async Task UnrelatedTransactionsDoNotConflict(IDgraphClient client) {

            var txn1 = client.NewTransaction();
            var txn2 = client.NewTransaction();

            var personTxn1 = MintAPerson("Alfred Name");
            var personTxn2 = MintAPerson("Fank Person");

            // Name has term and exact indexes, so these shouldn't clash
            var transactionResultTxn1 = await txn1.Mutate(JsonConvert.SerializeObject(personTxn1));
            AssertResultIsSuccess(transactionResultTxn1);
            personTxn1.Uid = transactionResultTxn1.Value[personTxn1.Uid.Substring(2)];

            var transactionResultTxn2 = await txn2.Mutate(JsonConvert.SerializeObject(personTxn2));
            AssertResultIsSuccess(transactionResultTxn2);
            personTxn2.Uid = transactionResultTxn2.Value[personTxn2.Uid.Substring(2)];
            
            // Can't see the other result
            var queryByName = await txn2.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", personTxn1.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");

            // Cause the mutates can't clash these should be ok
            AssertResultIsSuccess(await txn1.Commit());
            AssertResultIsSuccess(await txn2.Commit());

            // both are there
            queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", personTxn1.Name } });
            AssertResultIsSuccess(queryByName);
            FriendQueries.AssertStringIsPerson(queryByName.Value, personTxn1);

            queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", personTxn2.Name } });
            AssertResultIsSuccess(queryByName);
            FriendQueries.AssertStringIsPerson(queryByName.Value, personTxn2);
        }

        private async Task ConflictingTransactionsDontBothSucceed(IDgraphClient client) {

            var txn1 = client.NewTransaction();
            var txn2 = client.NewTransaction();

            var personTxn1 = MintAPerson("Bill Person");
            var personTxn2 = MintAPerson("Jane Person");

            var transactionResultTxn1 = await txn1.Mutate(JsonConvert.SerializeObject(personTxn1));
            AssertResultIsSuccess(transactionResultTxn1);
            personTxn1.Uid = transactionResultTxn1.Value[personTxn1.Uid.Substring(2)];

            var transactionResultTxn2 = await txn2.Mutate(JsonConvert.SerializeObject(personTxn2));
            AssertResultIsSuccess(transactionResultTxn2);

            // Name has term and exact indexes, so these clash on 'Person' term
            AssertResultIsSuccess(await txn1.Commit());
            var txn2Result = await txn2.Commit();
            txn2Result.IsFailed.Should().BeTrue();
            // Not 100% sure about this quirk.  Pretty sure this is how is how
            // dgo does it, but I wonder that it should be in an error state?
            txn2.TransactionState.Should().Be(TransactionState.Committed);

            // txn1 is there
            var queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", personTxn1.Name } });
            AssertResultIsSuccess(queryByName);
            FriendQueries.AssertStringIsPerson(queryByName.Value, personTxn1);

            // txn2 had no effect
            queryByName = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", personTxn2.Name } });
            AssertResultIsSuccess(queryByName);
            queryByName.Value.Should().Be("{\"q\":[]}");
        }

        #endregion

        private Person MintAPerson(string name) {
            return new Person() {
                Uid = "_:person",
                Name = name,
                Dob = new DateTime(1991, 1, 1),
                Height = 1.78,
                Scores = { 1, 32, 45, 62 }
            };
        }

    }
}