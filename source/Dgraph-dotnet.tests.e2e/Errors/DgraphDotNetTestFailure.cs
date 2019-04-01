using System;
using FluentResults;

namespace Dgraph_dotnet.tests.e2e.Errors
{
    public class DgraphDotNetTestFailure : Exception
    {
        private readonly ResultBase FailureReason;

        public DgraphDotNetTestFailure(string message) : base(message) { }

        public DgraphDotNetTestFailure(string message, ResultBase failureReason) : base(message) { 
            FailureReason = failureReason;
        }
    }
}