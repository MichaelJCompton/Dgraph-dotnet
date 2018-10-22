using System;
using System.Collections.Generic;
using System.Linq;

namespace DgraphDotNet.DgraphSchema {

    /*
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

    public class DrgaphPredicate {

        public readonly string Name;
        public readonly string Type;
        public bool IsIndexed => Tokenizers.Any();
        readonly List<string> tokenizers;
        public IReadOnlyList<string> Tokenizers => tokenizers;
        public readonly bool HasReverse;
        public readonly bool Countable;
        public readonly bool IsList;
        public readonly bool AllowUpsert;
        public readonly bool AllowLangTags;


        public DrgaphPredicate(
            string name, 
            string type, 
            IEnumerable<string> tokenizers, 
            bool hasReverse = false, 
            bool countable = false, 
            bool isList = false, 
            bool allowUpsert = false, 
            bool allowLangTags = false) {
                Name = name;
                Type = type;
                this.tokenizers = new List<string>(tokenizers);
                HasReverse = hasReverse;
                Countable = countable;
                IsList =isList;
                AllowUpsert = allowUpsert;
                AllowLangTags = allowLangTags;
        }

        public override string ToString() {
            string indexFragment = "";
            if(IsIndexed) {
                
                indexFragment = "@index(" + String.Join(",", Tokenizers) + ") ";
            }
            var reverseFragment = HasReverse ? "@reverse " : "";
            var countableFragment = Countable ? "@count " : "";
            var typeFragment = IsList ? $"[{Type}]" : $"{Type}";
            var upsertFragment = AllowUpsert ? "@upsert " : "";
            var langtagsFragment = AllowLangTags ? "@lang " : "";

            return $"{Name}: {typeFragment} {indexFragment}{reverseFragment}{countableFragment}{upsertFragment}{langtagsFragment}.";
        }
    }
}