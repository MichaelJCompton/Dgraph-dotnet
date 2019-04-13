using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DgraphDotNet;
using FluentResults;
using GraphSchema.io.Client;
using GraphSchema.io.Client.Models;
using Grpc.Core;
using Microsoft.Net.Http.Headers;
using Serilog;

namespace Dgraph_dotnet.tests.e2e.Orchestration {
    public class DgraphClientFactory : IDisposable {

        private readonly GraphSchemaIOConnection ConnectionConfig;

        private readonly IGraphSchemaIOClient GSioClient;

        private DgraphInstance GSioDgraph;

        // I'll need to store the GraphSchmaIO connection info to the actual
        // instance and creds in here

        public DgraphClientFactory(GraphSchemaIOConnection connectionConfig, IGraphSchemaIOClient GSioClient) {
            ConnectionConfig = connectionConfig;
            this.GSioClient = GSioClient;
        }

        public async Task<Result> ProvisionDgraph() {
            if (!ConnectionConfig.Endpoint.Equals("localhost")) {

                var envResult = await GSioClient.QueryEnvironment("Test");
                if (envResult.IsFailed) {
                    return envResult.ToResult();
                }

                var env = envResult.Value.FirstOrDefault();
                if (env == null) {
                    return Results.Fail("Environment not found");
                }

                var dgInput = new DgraphInstanceInput() {
                    Replicas = 1,
                    Shards = 1,
                    StorageGB = 2,
                    Env = new EnvironmentReference { Id = env.Id }
                };

                var dgresult = await GSioClient.AddDgraphInstanceAndWait(dgInput);

                if (dgresult.IsFailed) {
                    return dgresult.ToResult();
                }

                GSioDgraph = dgresult.Value;
                await CheckVersion();
            }
            return Results.Ok();
        }

        public async Task DestroyDgraph() {
            if (!ConnectionConfig.Endpoint.Equals("localhost") && GSioDgraph != null) {
                await GSioClient.DeleteDgraphInstance(GSioDgraph.Id);
            }
        }

        public IDgraphClient GetDgraphClient() {
            if (ConnectionConfig.Endpoint.Equals("localhost")) {
                var client = DgraphDotNet.Clients.NewDgraphClient();
                client.Connect("127.0.0.1:9080");
                return client;
            } else {
                var client = DgraphDotNet.Clients.NewDgraphClient();

                var caCert = GSioDgraph.Certificates.CaCert;
                var clientCert = GSioDgraph.Certificates.ClientCert;
                var clientKey = GSioDgraph.Certificates.ClientKey;
                var tls = new SslCredentials(caCert, new KeyCertificatePair(clientCert, clientKey));

                client.Connect(GSioDgraph.Address, tls);
                return client;
            }
        }

        public IDgraphMutationsClient GetMutationsClient() {
            if (ConnectionConfig.Endpoint.Equals("localhost")) {
                var client = DgraphDotNet.Clients.NewDgraphMutationsClient("127.0.0.1:5080");
                client.Connect("127.0.0.1:9080");
                return client;
            } else {
                throw new NotImplementedException();
            }
        }

        public IDgraphBatchingClient GetBatchingClient() {
            if (ConnectionConfig.Endpoint.Equals("localhost")) {
                var client = DgraphDotNet.Clients.NewDgraphBatchingClient("127.0.0.1:5080");
                client.Connect("127.0.0.1:9080");
                return client;
            } else {
                throw new NotImplementedException();
            }
        }

        private async Task CheckVersion() {
            using(var client = GetDgraphClient()) {
                // ATM GraphSchema.io deploys out the Dgraphs, but doesn't wait
                // till it's all up before returning. So it is possible for the
                // infrastructure to still be spinning up after a Dgraph is
                // returned - you'd get: 
                //
                // "Status(StatusCode=Unknown, Detail=\"Please retry again,
                // server is not ready to accept requests\")".  
                //
                // Don't think waiting for this should be built into the
                // IGraphSchemaIOClient, cause then it will only work with C#
                // clients.  For now, I'll leave it as client responsiblity to
                // check, but really GraphSchema.io shouldn't return the
                // instance untill it can verify that it's all awake. But that's
                // another layer of infrastructure, so for now, clients test.
                for (int i = 1; i < 4; i++) {
                    var result = await client.CheckVersion();
                    if (result.IsSuccess) {
                        Log.Information("Connected to Dgraph version {Version}", result.Value);
                        return;
                    }
                    // should use Polly and backoff?
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
            }
        }

        // 
        // ------------------------------------------------------
        //              disposable pattern.
        // ------------------------------------------------------
        //
        #region disposable pattern

        private bool Disposed;

        public void Dispose() {
            if (!Disposed) {
                // Ping GraphSchema.io to say I'm done with this Dgraph instance
                // No need to check that it's been cleaned up: as long as I got
                // a yes from GraphSchema.io, then it's its job to clean it up.
            }
            Disposed = true;
        }

        #endregion
    }
}