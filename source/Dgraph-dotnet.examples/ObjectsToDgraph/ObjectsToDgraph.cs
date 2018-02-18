using System;
using DgraphDotNet;
using FluentResults;
using Newtonsoft.Json;

namespace ObjectsExample {
    class ObjectsToDgraph {

        static string query =
            @"{
  find_Amit(func: allofterms(name@., ""Amit"")) {
    uid
    name@.
    friends {
      name@.
    }
  }
}";
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            Person Peter = new Person() {
                UID = "_:Peter",
                name = "Peter",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 15 + 234)
            };

            Person Mary = new Person() {
                name = "Mary",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 17 + 145)
            };

            Person Amit = new Person() {
                name = "Amit",
                DOB = DateTime.Now - TimeSpan.FromDays(365 * 21 + 67)
            };

            Peter.friends.Add(Mary);
            Amit.friends.Add(Peter);
            Amit.friends.Add(Mary);

            using(var client = DgraphDotNet.Clients.NewDgraphClient()) {
                client.Connect("127.0.0.1:9080");

                client.AlterSchema(
                    "name: string @index(term) .\n"
                    + "DOB: dateTime .\n"
                    + "friends: uid @count .");

                using(var transaction = client.NewTransaction()) {
                    var json = JsonConvert.SerializeObject(Amit);
                    Console.WriteLine(json);

                    transaction.Mutate(json);

                    transaction.Commit();
                }

                using(var transaction = client.NewTransaction()) {
                    var res = transaction.Query(query);
                    Console.WriteLine("---Query Result---");
                    Console.WriteLine(res);

                    dynamic amit = JsonConvert.DeserializeObject(res.Value);

                    transaction.Delete($"{{\"uid\": \"{amit.find_Amit[0].uid}\"}}");

                    transaction.Commit();
                }
            }
        }
    }
}