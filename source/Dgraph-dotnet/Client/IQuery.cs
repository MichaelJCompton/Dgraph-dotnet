using System.Collections.Generic;
using System.Threading.Tasks;
using DgraphDotNet.Schema;

namespace DgraphDotNet
{
    public interface IQuery
    {
        /// <summary>
        /// Returns all predicates in the Dgraph schema.
        /// </summary>
        Task<FluentResults.Result<DgraphSchema>> SchemaQuery();

        /// <summary>
        /// Returns predicates in the Dgraph schema returned by the given schema query.
        /// </summary>
        Task<FluentResults.Result<DgraphSchema>> SchemaQuery(string schemaQuery);

        /// <summary>
        /// Run a query.
        /// </summary>
        Task<FluentResults.Result<string>> Query(string queryString);

        /// <summary>
        /// Run a query with variables.
        /// </summary>
        Task<FluentResults.Result<string>> QueryWithVars(string queryString, Dictionary<string, string> varMap);
    }
}