using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Orchestration;
using Dgraph_dotnet.tests.e2e.Tests.TestClasses;
using DgraphDotNet;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Upserts are built into the C# client using transactions, so there's no Dgraph
// behaviour here that's different to what's tested in TransactionTest.  This
// just guarantees that the client-side upsert helper is working properly. 

namespace Dgraph_dotnet.tests.e2e.Tests {

    public class UpsertTest : GraphSchemaE2ETest {

        private string Schema = @"
Username: string @index(exact) @upsert .
Password: password .
IsUser: bool .
";

        private string UsersByName = @"
query users($name: string) {
    q(func: eq(Username, $name)) {  
        uid
        Username
    } 
}";

        private User User1;

        public UpsertTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Setup() {
            await base.Setup();
            var alterSchemaResult =
                await ClientFactory.GetDgraphClient().AlterSchema(Schema);
            AssertResultIsSuccess(alterSchemaResult);

            User1 = MintAUser(nameof(User1));
        }

        public async override Task Test() {
            using(var client = ClientFactory.GetDgraphClient()) {
                await UpsertNewNodeReturnsFalse(client);
                await UpsertExistingNodeReturnsTrue(client);
                await ConcurrentUpsertsWorkCorrectly(client);
            }
        }

        private async Task UpsertNewNodeReturnsFalse(IDgraphClient client) {
            User1.uid = "_:myBlank";
            var upsertResult =
                await client.Upsert(
                    nameof(User.Username),
                    GraphValue.BuildStringValue(User1.Username),
                    JsonConvert.SerializeObject(User1),
                    "myBlank");

            AssertResultIsSuccess(upsertResult);

            var(node, existing) = upsertResult.Value;
            existing.Should().BeFalse();
            User1.uid = string.Format("0x{0:x}", node.UID);
        }

        private async Task UpsertExistingNodeReturnsTrue(IDgraphClient client) {
            var upsertResult =
                await client.Upsert(
                    nameof(User.Username),
                    GraphValue.BuildStringValue(User1.Username),
                    JsonConvert.SerializeObject(User1),
                    "myBlank");

            AssertResultIsSuccess(upsertResult);

            var(node, existing) = upsertResult.Value;
            existing.Should().BeTrue();
            string.Format("0x{0:x}", node.UID).Should().Be(User1.uid);
        }

        private async Task ConcurrentUpsertsWorkCorrectly(IDgraphClient client) {

            var user2 = MintAUser("User2");
            user2.uid = "_:myBlank";
            var predicate = nameof(User.Username);
            var username = GraphValue.BuildStringValue(user2.Username);
            var json = JsonConvert.SerializeObject(user2);

            var tasks = Enumerable.Range(0, 10).
            Select(i => client.Upsert(
                predicate,
                username,
                json,
                "myBlank"));

            var results = await Task.WhenAll(tasks.ToList());

            foreach (var result in results) {
                AssertResultIsSuccess(result);
            }

            // Only one upsert should have acually written a value
            results.Select(r => r.Value.existing).Where(exists => !exists).Count().Should().Be(1);

            // Only one uid is ever seen
            var theUids = results.Select(r => r.Value.node.UID).Distinct();
            theUids.Count().Should().Be(1);

            // There's only one copy of user2 in the DB
            var queryResult = await client.QueryWithVars(
                UsersByName,
                new Dictionary<string, string> { { "$name", user2.Username } });
            AssertResultIsSuccess(queryResult);
            var users = (JArray) JObject.Parse(queryResult.Value) ["q"];
            users.Count.Should().Be(1);
            var user = users[0].ToObject<User>();
            string.Format("0x{0:x}", theUids.First()).Should().Be(user.uid);
            user.Username.Should().Be(user2.Username);
        }

        private User MintAUser(string name) {
            return new User() {
                uid = null,
                    Username = name,
                    Password = new Guid().ToString()
            };
        }

        private class User {
            public string uid { get; set; }
            public bool IsUser { get; set; } = true;
            public string Username { get; set; }
            public string Password { get; set; }
        }

    }
}