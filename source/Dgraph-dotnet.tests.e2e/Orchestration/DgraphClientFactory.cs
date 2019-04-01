using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DgraphDotNet;
using FluentResults;
using GraphSchema.io.Client;
using GraphSchema.io.Client.Models;
using Grpc.Core;
using Microsoft.Net.Http.Headers;

namespace Dgraph_dotnet.tests.e2e.Orchestration {
    public class DgraphClientFactory : IDisposable {

        private readonly GraphSchemaIOConnection ConnectionConfig;

        private readonly HttpClient HttpClient;

        private readonly GraphSchemaIOClient GSioClient;

        private DgraphInstance GSioDgraph;

        // I'll need to store the GraphSchmaIO connection info to the actual
        // instance and creds in here

        public DgraphClientFactory(GraphSchemaIOConnection connectionConfig) {
            ConnectionConfig = connectionConfig;

            if (!ConnectionConfig.Endpoint.Equals("localhost")) {
                HttpClient = new HttpClient();
                HttpClient.BaseAddress = new Uri(ConnectionConfig.Endpoint);

                HttpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"X-GraphSchemaIO-ApiKey {ConnectionConfig.ApiKeyId}:{ConnectionConfig.ApiKeySecret}");

                GSioClient = new GraphSchemaIOClient(HttpClient);
            }
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
            }
            return Results.Ok();
        }

        public async Task DestroyDgraph() {
            if (!ConnectionConfig.Endpoint.Equals("localhost")) {
                await GSioClient.DeleteDgraphInstance(GSioDgraph.DgraphId);
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

                client.Connect("...GraphSchema.io Dgraph address...", tls);
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