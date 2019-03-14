using System;
using System.Collections.Generic;
using System.Linq;

namespace DgraphDotNet.Schema {

    /*
    This message and the schema result payload are getting removed from Dgraph
    (deprecated at v1.0.12), but this is a useful way to return the schema, so
    I'll try and keep it

    message SchemaNode {
    	string predicate = 1;
    	string type = 2;
    	bool index = 3;
    	repeated string tokenizer = 4;
    	bool reverse = 5;
    	bool count = 6;
    	bool list = 7;
    	bool upsert = 8;
    	bool lang = 9;
    }
    */

    // I don't really know I want to take a dependecy on JSON.Net in the library?
    // For the moment it looks like the only way.  Looks like high perf 
    // deserialisation support might be baked into net core soon though
    // https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#fast-built-in-json-support
    // Looks like second halfo of 2019 https://devblogs.microsoft.com/dotnet/announcing-net-core-3-preview-3/
    // That would allow to do this quick bit of deserialization and allow clients to use
    // whatever they want or maybe to have query<T> types supported without dependency?  Might
    // add it soon anyway with JSON.Net.
    public class DrgaphPredicate {
        public string Predicate { get; set; }
        public string Type { get; set; }
        public bool Index { get; set; }
        public List<string> Tokenizer { get; set; }
        public bool Reverse { get; set; }
        public bool Count { get; set; }
        public bool List { get; set; }
        public bool Upsert { get; set; }
        public bool Lang { get; set; }

        public override string ToString() {
            string indexFragment = "";
            if (Index) {

                indexFragment = "@index(" + String.Join(",", Tokenizer) + ") ";
            }
            var reverseFragment = Reverse ? "@reverse " : "";
            var countableFragment = Count ? "@count " : "";
            var typeFragment = List ? $"[{Type}]" : $"{Type}";
            var upsertFragment = Upsert ? "@upsert " : "";
            var langtagsFragment = Lang ? "@lang " : "";

            return $"{Predicate}: {typeFragment} {indexFragment}{reverseFragment}{countableFragment}{upsertFragment}{langtagsFragment}.";
        }
    }
}