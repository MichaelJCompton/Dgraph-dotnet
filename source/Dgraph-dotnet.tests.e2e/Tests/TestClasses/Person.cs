using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dgraph_dotnet.tests.e2e.Tests.TestClasses
{

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Person
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public List<Person> Friends { get; } = new List<Person>();
        public DateTime Dob { get; set; }
        public double Height { get; set; }
        public List<int> Scores { get; } = new List<int>();
    }

}
