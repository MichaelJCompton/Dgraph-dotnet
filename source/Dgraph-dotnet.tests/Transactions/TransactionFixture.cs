using System.Collections.Generic;
using System.Linq;
using System.Text;
using Api;
using DgraphDotNet;
using DgraphDotNet.Transactions;
using FluentAssertions;
using FluentResults;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Transactions {
    public class TransactionFixture {

        // 
        // ------------------------------------------------------
        //                   ITransaction
        // ------------------------------------------------------
        //

        #region Mutate

        #endregion

        #region Delete

        #endregion

        #region Discard

        #endregion

        #region Commit

        #endregion

        // 
        // ------------------------------------------------------
        //                   Other
        // ------------------------------------------------------
        //

        public void All_ExceptionIfDiscarded() {
            // grab everything from the interfaces
            // discard the transaction
            // loop through, should get an exception for each
        }
    }
}