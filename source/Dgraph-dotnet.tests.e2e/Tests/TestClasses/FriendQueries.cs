using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Dgraph_dotnet.tests.e2e.Tests.TestClasses {

    public class FriendQueries {
        
        public static string QueryByUid(string uid) =>
            "{  "
            + $"    q(func: uid({uid})) "
            + "     {   "
            + "        uid  "
            + "        name  "
            + "        dob  "
            + "        height  "
            + "        scores  "
            + "        friends {   "
            + "            uid  "
            + "            name  "
            + "            dob  "
            + "            height  "
            + "            scores   "
            + "        }   "
            + "    }   "
            + "}";

        public static string QueryByName = @"
query people($name: string) {
    q(func: eq(name, $name)) {
        uid
        name
        dob
        height
        scores
        friends {
            uid
            name
            dob
            height
            scores
        }
    }
}";

        public static void AssertStringIsPerson(string json, Person person) {
            var people = JObject.Parse(json) ["q"].ToObject<List<Person>>();
            people.Count.Should().Be(1);
            people[0].Should().BeEquivalentTo(person);
        }

    }
}