using System;
namespace DgraphDotNet.Graph {
    public class Property : GraphLink<GraphValue> {

            /// <remarks> Precondition : <paramref name="source"/> and <paramref name="value"/> are not null and <paramref name="name"/> is not null or "".</remarks>
            internal Property(INode source, string propertyName, GraphValue value) : base(source, propertyName, value) { }
        }
}