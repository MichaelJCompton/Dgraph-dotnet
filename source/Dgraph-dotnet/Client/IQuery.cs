using System.Collections.Generic;
using DgraphDotNet.DgraphSchema;

namespace DgraphDotNet
{
    public interface IQuery
    {
        /// <summary>
        /// Returns all predicates in the Dgraph schema.
        /// </summary>
        FluentResults.Result<IReadOnlyList<DrgaphPredicate>> SchemaQuery();

        /// <summary>
        /// Returns predicates in the Dgraph schema returned by the given schema query.
        /// </summary>
        FluentResults.Result<IReadOnlyList<DrgaphPredicate>> SchemaQuery(string schemaQuery);

        /// <summary>
        /// Run a query in it's own transaction.
        /// </summary>
        FluentResults.Result<string> Query(string queryString);

        /// <summary>
        /// Run a query in it's own transaction.
        /// </summary>
        FluentResults.Result<string> QueryWithVars(string queryString, Dictionary<string, string> varMap);
    }
}