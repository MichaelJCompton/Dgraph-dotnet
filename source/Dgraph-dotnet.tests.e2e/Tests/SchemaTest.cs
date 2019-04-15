using System.Threading.Tasks;
using Assent;
using Dgraph_dotnet.tests.e2e.Errors;
using Dgraph_dotnet.tests.e2e.Orchestration;
using DgraphDotNet;
using FluentAssertions;

namespace Dgraph_dotnet.tests.e2e.Tests {
    public class SchemaTest : GraphSchemaE2ETest {
        public SchemaTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Test() {
            using(var client = ClientFactory.GetDgraphClient()) {
                await InitialSchemaIsAsExpected(client);
                await AlterSchemAsExpected(client);
                await AlterSchemaAgainAsExpected(client);
                await SchemaQueryWithRestrictions(client);

                await ErrorsResultInFailedQuery(client);
            }
        }

        private async Task InitialSchemaIsAsExpected(IDgraphClient client) {
            var schemaResult = await client.SchemaQuery();
            AssertResultIsSuccess(schemaResult);
            this.Assent(schemaResult.Value.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemAsExpected(IDgraphClient client) {
            var alterSchemaResult = await client.AlterSchema(ReadEmbeddedFile("test.schema"));
            AssertResultIsSuccess(alterSchemaResult);

            var schemaResult = await client.SchemaQuery();
            AssertResultIsSuccess(schemaResult);
            this.Assent(schemaResult.Value.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemaAgainAsExpected(IDgraphClient client) {
            var alterSchemaResult = await client.AlterSchema(ReadEmbeddedFile("altered.schema"));
            AssertResultIsSuccess(alterSchemaResult);

            var schemaResult = await client.SchemaQuery();
            AssertResultIsSuccess(schemaResult);
            this.Assent(schemaResult.Value.ToString(), AssentConfiguration);
        }

        private async Task SchemaQueryWithRestrictions(IDgraphClient client) {
            var schemaResult = await client.SchemaQuery("schema(pred: [name, friends, dob, scores]) { type }");
            AssertResultIsSuccess(schemaResult);
            this.Assent(schemaResult.Value.ToString(), AssentConfiguration);
        }

        private async Task ErrorsResultInFailedQuery(IDgraphClient client) {
            // maformed
            var q1result = await client.SchemaQuery("schema(pred: [name, friends, dob, scores]) { type ");
            q1result.IsSuccess.Should().BeFalse();

            // not a schema query
            var q2result = await client.SchemaQuery("{ q(func: uid(0x1)) { not-schema-query } }");
            q2result.IsSuccess.Should().BeFalse();
        }
    }
}