using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ObjectsExample
{
    public class Person
    {
        // Doesn't matter how this is done, but it must be uid in the JSON, not
        // Uid, not UID.
        [JsonProperty("uid")]
        public string UID { get; set; } // uid should be a string

        public string Name { get; set; }
        public DateTime DOB { get; set; }
        public List<Person> Friends { get; } = new List<Person>();
    }
}