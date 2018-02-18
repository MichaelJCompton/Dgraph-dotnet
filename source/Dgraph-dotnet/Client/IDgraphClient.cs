using System;
using System.Collections.Generic;
using DgraphDotNet.Transactions;


namespace DgraphDotNet {

    /// <summary>
    /// A client is in charge of making connections to Dgraph backends.  Once a connection is made,
    /// the client manages the connection (and shuts it down on exit).
    /// 
    /// This type of client can do transactions with query and JSON mutations.
    /// see : https://docs.dgraph.io/mutations/#json-mutation-format
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Thrown if the client has been disposed and calls are made.</exception>
    public interface IDgraphClient : IDisposable {

        /// <summary>
        /// Connect to a backend Dgraph instance.  Multiple connections can be made in a single client.
        /// </summary>
        /// <param name="address"> address to connect to: e.g. of the form 127.0.0.1:9080.</param>
        /// <remarks>All addresses added to a single client should be addresses of 
        /// servers in a single Dgraph cluster.   On running a call such as <see cref="AlterSchema(string)"/>
        /// or submitting an <see cref="ITransaction"/> any one of the connections is used.
        /// </remarks>
        void Connect(string address);

        IEnumerable<string> AllConnections();

        /// <summary>
        /// Alter the schema see: https://docs.dgraph.io/query-language/#schema
        /// </summary>
        void AlterSchema(string newSchema);

        ITransaction NewTransaction();

        // FIXME: To come are options for TLS, setting policy for retries etc and managing connections
        //
        // FIXME: Also more error handling rather than throwing exceptions for everything (batching client
        // might be better like that anyway).
    }
}