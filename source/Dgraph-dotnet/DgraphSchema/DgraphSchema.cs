using System.Collections.Generic;
using System.Linq;

namespace DgraphDotNet.Schema {

    public class DgraphSchema {
        public List<DrgaphPredicate> Schema { get; set; }

        public override string ToString() =>
            string.Join("\n", Schema.Select(p => p.ToString()));
    }
}