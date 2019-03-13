/** 
 * Example of using Dgraph client with object serialization and query.
 *
 * Running :
 *  1) start up dgraph (e.g. see ../scripts/server.sh)
 *  2) dotnet run
 *
 */



using System;
using DgraphDotNet;
using FluentResults;
using Newtonsoft.Json;

namespace ObjectsExample {
    class ObjectsToDgraph {

        static string query =
            @"{
  find_Amit(func: allofterms(Name@., ""Amit"")) {
    uid
    Name@.
    Friends {
      uid
      Name@.
    }
  }
}";

        static void Main(string[] args) {

            // If uid is left blank a new node will be allocated in the graph.
            // If it's set to a known uid, new properties will be added to that
            // existing node.
            //
            // ...and if a uid is set in the JSON as a blank node like this
            // ("_:Peter"), then a new node will be allocated and the response
            // will have a uid mapping showing what uid "Peter" was allocated
            // (rather than something like "blank-0").     
            Person Peter = new Person() {
                UID = "_:Peter",
                Name = "Peter",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 15 + 234)
            };

            Person Mary = new Person() {
                Name = "Mary",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 17 + 145)
            };

            Person Amit = new Person() {
                Name = "Amit",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 21 + 67)
            };

            Peter.Friends.Add(Mary);
            Amit.Friends.Add(Peter);
            Amit.Friends.Add(Mary);

            using(var client = DgraphDotNet.Clients.NewDgraphClient()) {
                client.Connect("127.0.0.1:9080");

                client.AlterSchema(
                    "Name: string @lang @index(term) .\n"
                    + "DOB: dateTime .\n"
                    + "Friends: uid @count .");

                // 
                // Add some data to Dgraph
                //
                using(var transaction = client.NewTransaction()) {

                    // Serialize object to JSON in whatever way works for you
                    // and your schema.
                    var json = JsonConvert.SerializeObject(Amit);

                    Console.WriteLine("Input json for mutation : " + json);

                    // the payload of the result is a name->uid map of newly
                    // allocated nodes
                    var result = transaction.Mutate(json);                
                    if(result.IsFailed) {
                        Console.WriteLine("Something went wrong : " + result);
                        System.Environment.Exit(1);
                    }
                    // result.IsSuccess
                    Console.WriteLine("Result from mutation : " + result);
                    Console.WriteLine("uid allocated for Peter is : " + result.Value["Peter"]);
                    transaction.Commit();
                }

                // 
                // Query some data from Dgraph
                // Change the data
                //
                using(var transaction = client.NewTransaction()) {

                    // a query returns the JSON result
                    var result = transaction.Query(query);
                    if(result.IsFailed) {
                        Console.WriteLine("Something went wrong : " + result);
                        System.Environment.Exit(1);
                    }

                    // query result JSON is structured just like the query
                    Console.WriteLine("Query Result : " + result);

                    dynamic amit = JsonConvert.DeserializeObject(result.Value);

                    // delete a node by passing JSON like this to delete
                    transaction.Delete($"{{\"uid\": \"{amit.find_Amit[0].uid}\"}}");

                    transaction.Commit();
                }
            }
        }
    }
}