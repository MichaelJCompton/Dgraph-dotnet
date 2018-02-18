using System;
using System.Collections.Generic;

namespace ObjectsExample
{
    public class Person
    {
        public string UID { get; set; }
        public string name { get; set; }
        public DateTime DOB { get; set; }
        public List<Person> friends { get; } = new List<Person>();
    }
}