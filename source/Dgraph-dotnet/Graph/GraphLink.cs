using System;
using System.Collections.Generic;
using System.Diagnostics;
using Api;

namespace DgraphDotNet.Graph {
    public abstract class GraphLink<T> where T : IEdgeTarget {

        public INode Source { get; }
        public T Target { get; }
        public string Name { get; set; }

        /// <summary>
        /// Facets are (key, value) properties on graph links.
        /// </summary>
        /// <remarks> Facets in Dgraph are always string-string, but server side Dgraph will try to parse out values for ints, dates, etc.</remarks>
        public readonly Dictionary<string, string> Facets = new Dictionary<string, string>();

        /// <remarks> Precondition : <paramref name="source"/> and <paramref name="target"/> are not null and <paramref name="name"/> is not null or "".</remarks>

        protected internal GraphLink(INode source, string name, T target) {
            Debug.Assert(source != null);
            Debug.Assert(!ReferenceEquals(target, null));
            Debug.Assert(!string.IsNullOrEmpty(name));

            Source = source;
            Target = target;
            Name = name;
        }
    }
}