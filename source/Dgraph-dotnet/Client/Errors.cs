using System;
using FluentResults;
using Grpc.Core;

namespace DgraphDotNet {

    public class TransactionFinished : Error {
        internal TransactionFinished(string state) : base("Cannot perform action when transaction is in state " + state) {

        }
    }

    public class StartTsMismatch : Error {
        internal StartTsMismatch() : base("StartTs mismatch") {

        }
    }

    public class BadArgs : Error {
        internal BadArgs(string message) : base(message) {
            
        }
    }
}