/** 
 * Example of using Dgraph client with individual mutations.
 * Simple example of upserts and properties.
 *
 * Running :
 *  1) start up dgraph (e.g. see ../scripts/server.sh)
 *  2) dotnet run
 *
 */

using System;
using System.Globalization;
using System.Threading.Tasks;
using DgraphDotNet;
using DgraphDotNet.Graph;
using FluentResults;

namespace MutationExamples {
    class Program {
        static async Task Main(string[] args) {

            using(IDgraphMutationsClient client = DgraphDotNet.Clients.NewDgraphMutationsClient("127.0.0.1:5080")) {
                client.Connect("127.0.0.1:9080");

                await client.AlterSchema(
                    "Username: string @index(hash) .\n"
                    + "Password: password .");

                var schemaResult = await client.SchemaQuery();
                if(schemaResult.IsFailed) {
                    Console.WriteLine($"Something went wrong getting schema.");
                    return;
                }

                Console.WriteLine("Queried schema and got :");
                foreach (var predicate in schemaResult.Value.Schema) {
                    Console.WriteLine(predicate.ToString());
                }

                while (true) {
                    Console.WriteLine("Hi, please enter your new username");
                    var username = Console.ReadLine();

                    // use Upsert to test for a node and value, and create if
                    // not already in the graph as an atomic operation.
                    var result = await client.Upsert("Username", GraphValue.BuildStringValue(username), $"{{\"Username\": \"{username}\"}}");

                    if (result.IsFailed) {
                        Console.WriteLine("Something went wrong : " + result);
                        continue;
                    }

                    var(node, existed) = result.Value;

                    if (existed) {
                        Console.WriteLine("This user already existed.  Try another username.");
                        continue;
                    }

                    Console.WriteLine("Hi, please enter a password for the new user");
                    var password = Console.ReadLine();

                    using(var txn = client.NewTransactionWithMutations()) {
                        var mutation = txn.NewMutation();
                        var property = Clients.BuildProperty(node, "Password", GraphValue.BuildPasswordValue(password));
                        if (property.IsFailed) {
                            // ... something went wrong
                        } else {
                            mutation.AddProperty(property.Value);
                            var err = await mutation.Submit();
                            if (err.IsFailed) {
                                // ... something went wrong
                            }
                        }
                        await txn.Commit();
                    }
                }
            }
        }
    }
}