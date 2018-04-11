using System;
using System.Globalization;
using DgraphDotNet;
using DgraphDotNet.Graph;
using FluentResults;

namespace MutationExamples {
    class Program {
        static void Main(string[] args) {

            // var numericString = "8F8C";
            // CallTryParse(numericString, NumberStyles.HexNumber);

            using(IDgraphMutationsClient client = DgraphDotNet.Clients.NewDgraphMutationsClient("127.0.0.1:5080")) {
                client.Connect("127.0.0.1:9080");

                client.AlterSchema("name: string @index(term) .");

                var res = client.Upsert("name", GraphValue.BuildStringValue("james"));
                if(res.IsSuccess) {
                    Console.WriteLine("asdfdsaf");
                }

                using(var txn = client.NewTransaction()) {
                    
                }
            }
        }

        private static void CallTryParse(string stringToConvert, NumberStyles styles) {
            ulong number;
            var result = UInt64.Parse(stringToConvert, styles,
                CultureInfo.InvariantCulture); //, out number);
            // if (result)
            number = result;
                 Console.WriteLine("Converted '{0}' to {1}.", stringToConvert, number);
            // else
            //     Console.WriteLine("Attempted conversion of '{0}' failed.",
            //         Convert.ToString(stringToConvert));
        }
    }
}