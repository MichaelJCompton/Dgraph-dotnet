using System;
using DgraphDotNet;

namespace DgraphDotNet.Graph {
    public class Edge : GraphLink<INode> {

            /// <remarks> Precondition : <paramref name="source"/> and <paramref name="target"/> are not null and <paramref name="name"/> is not null or "".</remarks>
            internal Edge(INode source, string name, INode target) : base(source, name, target) {

            }

        }
}