using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grpc.Core;

namespace DgraphDotNet {

    internal interface IGRPCConnectionFactory {
        bool TryConnect(string address, out IGRPCConnection connection, ChannelCredentials credentials = null, IEnumerable<ChannelOption> options = null);
    }

    internal class GRPCConnectionFactory : IGRPCConnectionFactory {

        public GRPCConnectionFactory() {

        }

        /// <summary>Connect on the given address.  I don't think this fails??
        /// on bad addresses grpc still returns a connection, but will throw
        /// exception on use. </summary>
        /// <remarks>Pre : <c>!string.IsNullOrEmpty(address)</c>. </remarks>
        public virtual bool TryConnect(
            string address,
            out IGRPCConnection connection,
            ChannelCredentials credentials = null,
            IEnumerable<ChannelOption> options = null) {
                
            Debug.Assert(!string.IsNullOrEmpty(address));

            try {
                Channel channel = new Channel(address, credentials ?? ChannelCredentials.Insecure, options);

                Api.Dgraph.DgraphClient client = new Api.Dgraph.DgraphClient(channel);

                connection = new GRPCConnection(channel, client);

                return true;
            } catch (RpcException) {
                // FIXME: log this.  If I ever get this error, then I might
                // need to do more here to return that error to the client.
                // actually use the error type
            }
            connection = null;
            return false;
        }

    }
}