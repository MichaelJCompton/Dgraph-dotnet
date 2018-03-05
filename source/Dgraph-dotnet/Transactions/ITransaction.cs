using System;
using System.Collections.Generic;


namespace DgraphDotNet.Transactions
{

    public interface ITransaction : IDisposable {
        bool Committed { get; }
        bool Aborted { get; }
        bool HasError { get; }
        bool IsOK { get; }
        FluentResults.Result<string> Query(string queryString);
        FluentResults.Result<string> QueryWithVars(string queryString, Dictionary<string, string> varMap);
        FluentResults.Result<IDictionary<string, string>> Mutate(string json);
        FluentResults.Result Delete(string json);
        void Discard();
        FluentResults.Result Commit();
    }
}