using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Errors;
using Dgraph_dotnet.tests.e2e.Orchestration;
using Dgraph_dotnet.tests.e2e.Tests.TestClasses;
using DgraphDotNet;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dgraph_dotnet.tests.e2e.Tests {
    public class MutateQueryTest : GraphSchemaE2ETest {

        private Person Person1, Person2, Person3;

        public MutateQueryTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Setup() {
            await base.Setup();
            var alterSchemaResult = await ClientFactory.GetDgraphClient().AlterSchema(ReadEmbeddedFile("test.schema"));
            AssertResultIsSuccess(alterSchemaResult);

            Person1 = new Person() {
                Uid = "_:Person1",
                Name = "Person1",
                Dob = new DateTime(1991, 1, 1),
                Height = 1.78,
                Scores = { 1, 32, 45, 62 }
            };

            Person2 = new Person() {
                Uid = "_:Person2",
                Name = "Person2",
                Dob = new DateTime(1992, 1, 1),
                Height = 1.85,
                Scores = { 3, 2, 1 }
            };

            Person3 = new Person() {
                Uid = "_:Person3",
                Name = "Person3",
                Dob = new DateTime(1993, 1, 1),
                Height = 1.85,
                Scores = { 32, 21, 10 },
            };
            Person3.Friends.AddRange(new List<Person> { Person1 });
        }

        public async override Task Test() {
            using(var client = ClientFactory.GetDgraphClient()) {
                await AddThreePeople(client);
                await QueryAllThreePeople(client);
                await AlterAPerson(client);
                await QueryWithVars(client);
                await DeleteAPerson(client);
            }
        }

        private async Task AddThreePeople(IDgraphClient client) {

            using(var transaction = client.NewTransaction()) {

                // Serialize the objects to json however works best for you.
                //
                // The CamelCaseNamingStrategy attribute on the type means these
                // get serialised with initial lowwer case.
                var personList = new List<Person> { Person2, Person3 };
                var json = JsonConvert.SerializeObject(personList);

                var result = await transaction.Mutate(json);
                AssertResultIsSuccess(result, "Mutation failed");

                // The payload of the result is a node->uid map of newly
                // allocated nodes.  If the nodes don't have uid names in the
                // mutation, then the map is like
                //
                // {{ "blank-0": "0xa", "blank-1": "0xb", "blank-2": "0xc", ... }}
                //
                // If the
                // mutation has '{ "uid": "_:Person1" ... }' etc, then the blank
                // node map is like
                // 
                // {{ "Person3": "0xe", "Person1": "0xf", "Person2": "0xd", ... }}

                result.Value.Count.Should().Be(3);

                // It's no required to save the uid's like this, but can work
                // nicely ... and makes these tests easier to keep track of.

                Person1.Uid = result.Value[Person1.Uid.Substring(2)];
                Person2.Uid = result.Value[Person2.Uid.Substring(2)];
                Person3.Uid = result.Value[Person3.Uid.Substring(2)];

                await transaction.Commit();
            }
        }

        private async Task QueryAllThreePeople(IDgraphClient client) {

            var people = new List<Person> { Person1, Person2, Person3 };

            foreach (var person in people) {
                var queryPerson = await client.Query(FriendQueries.QueryByUid(person.Uid));
                AssertResultIsSuccess(queryPerson, "Query failed");

                // the query result is json like { q: [ ...Person... ] }
                FriendQueries.AssertStringIsPerson(queryPerson.Value, person);
            }
        }

        private async Task AlterAPerson(IDgraphClient client) {
            using(var transaction = client.NewTransaction()) {
                Person3.Friends.Add(Person2);

                // This will serialize the whole object.  You might not want to
                // do that, and maybe only add in the bits that have changed
                // instead.
                var json = JsonConvert.SerializeObject(Person3);

                var result = await transaction.Mutate(json);
                AssertResultIsSuccess(result, "Mutation failed");

                // no nodes were allocated
                result.Value.Count.Should().Be(0);

                await transaction.Commit();
            }

            var queryPerson = await client.Query(FriendQueries.QueryByUid(Person3.Uid));
            AssertResultIsSuccess(queryPerson, "Query failed");

            FriendQueries.AssertStringIsPerson(queryPerson.Value, Person3);
        }

        private async Task QueryWithVars(IDgraphClient client) {

            var queryResult = await client.QueryWithVars(
                FriendQueries.QueryByName,
                new Dictionary<string, string> { { "$name", Person3.Name } });
            AssertResultIsSuccess(queryResult, "Query failed");

            FriendQueries.AssertStringIsPerson(queryResult.Value, Person3);
        }

        private async Task DeleteAPerson(IDgraphClient client) {
            using(var transaction = client.NewTransaction()) {

                // delete a node by passing JSON like this to delete
                var deleteResult = await transaction.Delete($"{{\"uid\": \"{Person1.Uid}\"}}");
                AssertResultIsSuccess(deleteResult, "Delete failed");

                await transaction.Commit();
            }

            // that person should be gone...
            var queryPerson1 = await client.Query(FriendQueries.QueryByUid(Person1.Uid));
            AssertResultIsSuccess(queryPerson1, "Query failed");

            // no matter what uid you query for, Dgraph always succeeds :-(
            // e.g. on a fresh dgraph with no uids allocated
            // { q(func: uid(0x44444444)) { uid }} 
            // will answer
            // "q": [ { "uid": "0x44444444" } ] 
            //
            // so the only way to test that the node is deleted, 
            // is to test that we got only that back

            queryPerson1.Value.Should().Be($"{{\"q\":[{{\"uid\":\"{Person1.Uid}\"}}]}}");

            // -----------------------------------------------------------
            // ... but watch out, Dgraph can leave dangling references :-(
            // -----------------------------------------------------------
            var queryPerson3 = await client.Query(FriendQueries.QueryByUid(Person3.Uid));
            AssertResultIsSuccess(queryPerson3, "Query failed");
            var person3 = JObject.Parse(queryPerson3.Value) ["q"][0].ToObject<Person>();

            person3.Friends.Count.Should().Be(2);

            // You'll need something like GraphSchema to handle things like
            // cascading deletes etc. and clean up automatically :-)
        }

    }
}