using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using Dgraph_dotnet.tests.e2e.Orchestration;
using DgraphDotNet;

namespace Dgraph_dotnet.tests.e2e.Tests {
    public class SchemaTest : GraphSchemaE2ETest {
        public SchemaTest(DgraphClientFactory clientFactory) : base(clientFactory) { }

        public async override Task Test() {
            using(var client = ClientFactory.GetDgraphClient()) {
                InitialSchemaIsAsExpected(client);
                await AlterSchemAsExpected(client);
                await AlterSchemaAgainAsExpected(client);
                SchemaQueryWithRestrictions(client);

                ErrorsResultInFailedQuery(client);
            }
        }

        private void InitialSchemaIsAsExpected(IDgraphClient client) {
            var schema = client.SchemaQuery();
            schema.IsSuccess.Should().BeTrue();
            this.Assent(schema.Value.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemAsExpected(IDgraphClient client) { 
            var alterSchemaResult = await client.AlterSchema(ReadEmbeddedFile("test.schema"));
            alterSchemaResult.IsSuccess.Should().BeTrue();
            var schema = client.SchemaQuery();
            schema.IsSuccess.Should().BeTrue();
            this.Assent(schema.Value.ToString(), AssentConfiguration);
        }

        private async Task AlterSchemaAgainAsExpected(IDgraphClient client) { 
            var alterSchemaResult = await client.AlterSchema(ReadEmbeddedFile("altered.schema"));
            alterSchemaResult.IsSuccess.Should().BeTrue();
            var schema = client.SchemaQuery();
            schema.IsSuccess.Should().BeTrue();
            this.Assent(schema.Value.ToString(), AssentConfiguration);
        }

        private void SchemaQueryWithRestrictions(IDgraphClient client) { 
            var schema = client.SchemaQuery("schema(pred: [name, friends, dob, scores]) { type }");
            schema.IsSuccess.Should().BeTrue();
            this.Assent(schema.Value.ToString(), AssentConfiguration);
        }

        private void ErrorsResultInFailedQuery(IDgraphClient client) { 
            // maformed
            var q1result = client.SchemaQuery("schema(pred: [name, friends, dob, scores]) { type ");
            q1result.IsSuccess.Should().BeFalse();

            // not a schema query
            var q2result = client.SchemaQuery("{ q(func: uid(0x1)) { not-schema-query } }");
            q2result.IsSuccess.Should().BeFalse();
        }
    }
}