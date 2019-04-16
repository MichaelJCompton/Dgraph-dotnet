using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dgraph_dotnet.tests.e2e.Errors;
using Dgraph_dotnet.tests.e2e.Orchestration;
using Dgraph_dotnet.tests.e2e.Tests;
using DgraphDotNet;
using GraphSchema.io.Client;
using GraphSchema.io.Client.Models;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Serilog;

namespace Dgraph_dotnet.tests.e2e {

    [Command(Name = "Dgraph-dotnet E2E test runner")]
    [HelpOption("--help")]
    class Program {

        [Option(ShortName = "t", Description = "Set the tests to actually run.  Can be set multiple times.  Not setting == run all tests.")]
        public List<string> Test { get; } = new List<string>();

        [Option(ShortName = "i", Description = "Turn on interactive mode when not running in build server.")]
        public bool Interactive { get; }

        [Option(ShortName = "p", Description = "Run parallel test executions.")]
        public int Parallel { get; } = 1;

        public static int Main(string[] args) {
            try {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional : false, reloadOnChange : false)
                    .AddUserSecrets<Program>()
                    .AddEnvironmentVariables("DGDNE2E_")
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                var services = new ServiceCollection();

                var graphschemaIOconnection = new GraphSchemaIOConnection();
                config.Bind(nameof(GraphSchemaIOConnection), graphschemaIOconnection);
                services.AddSingleton<GraphSchemaIOConnection>(graphschemaIOconnection);

                if (!graphschemaIOconnection.Endpoint.Equals("localhost")) {
                    services.AddGraphSchemaIOLClient(httpClient => {
                        httpClient.BaseAddress = new Uri(graphschemaIOconnection.Endpoint);

                        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization,
                            $"X-GraphSchemaIO-ApiKey {graphschemaIOconnection.ApiKeyId}:{graphschemaIOconnection.ApiKeySecret}");
                    });
                } else {
                    services.AddGraphSchemaIOLClient(httpClient => { }); // only needed to make DI happy
                }

                // Inject in every possible test type so that DI will be able to
                // mint up these for me without me having to do anything to hydrate
                // the objects.
                Type baseTestType = typeof(GraphSchemaE2ETest);
                var assembly = typeof(GraphSchemaE2ETest).Assembly;
                IEnumerable<Type> testTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(baseTestType));
                foreach (var testType in testTypes) {
                    services.AddTransient(testType);
                }

                services.AddSingleton<TestFinder>();
                services.AddTransient<TestExecutor>();
                services.AddScoped<DgraphClientFactory>();

                var serviceProvider = services.BuildServiceProvider();

                var app = new CommandLineApplication<Program>();
                app.Conventions
                    .UseDefaultConventions()
                    .UseConstructorInjection(serviceProvider);

                app.Execute(args);
                return 0;

            } catch (AggregateException aggEx) {
                foreach (var ex in aggEx.InnerExceptions) {
                    switch (ex) {
                        case DgraphDotNetTestFailure testEx:
                            Log.Error("Test Failed with reason {@Reason}", testEx.FailureReason);
                            Log.Error(testEx, "Call Stack");
                            break;
                        default:
                            Log.Error(ex, "Unknown Exception Failure");
                            break;
                    }
                }
            } catch (Exception ex) {
                Log.Error(ex, "Test run failed.");
            } finally {
                Log.CloseAndFlush();
            }
            return 1;
        }

        public Program(GraphSchemaIOConnection connectionInfo, IServiceProvider serviceProvider, TestFinder testFinder, TestExecutor testExecutor) {
            ConnectionInfo = connectionInfo;
            ServiceProvider = serviceProvider;
            TestFinder = testFinder;
            TestExecutor = testExecutor;
        }

        private GraphSchemaIOConnection ConnectionInfo;
        private IServiceProvider ServiceProvider;
        private TestFinder TestFinder;
        private TestExecutor TestExecutor;

        private async Task OnExecuteAsync(CommandLineApplication app) {

            if (ConnectionInfo.Endpoint.Equals("localhost") && Parallel > 1) {
                throw new ArgumentException("Local and Parallel execution are incompatible");
            }

            if (Interactive && Parallel > 1) {
                throw new ArgumentException("Interactive mode and Parallel execution are incompatible");
            }

            if (Parallel < 1) {
                throw new ArgumentException("Parallel must be greater than 1.");
            }

            EnsureAllTestsRegistered();

            var tests = TestFinder.FindTestNames(Test);

            var batchSize = tests.Count() / Parallel;

            var batches = new List<List<string>>();
            for (var i = 0; i < Parallel; i++) {
                batches.Add(tests.Skip(i * batchSize).Take(batchSize).ToList());
            }

            Log.Information("Begining {Parallel} parallel test runs with batches : {@Batches}", Parallel, batches);

            // Exceptions shouldn't escape this in normal circumstances.
            var executors = await Task.WhenAll(batches.Select(b => Execute(b)).ToList());

            var totalRan = executors.Select(ex => ex.TestsRun).Sum();
            var totalFailed = executors.Select(ex => ex.TestsFailed).Sum();
            var exceptionList = executors.SelectMany(ex => ex.Exceptions);

            Log.Information("-----------------------------------------");
            Log.Information("Test Results:");
            Log.Information($"Tests Run: {totalRan}");
            Log.Information($"Tests Succesful: {totalRan - totalFailed}");
            Log.Information($"Tests Failed: {totalFailed}");
            Log.Information("-----------------------------------------");

            if (totalFailed > 0) {
                throw new AggregateException(exceptionList);
            }
        }

        private async Task<TestExecutor> Execute(IEnumerable<string> tests) {
            using(ServiceProvider.CreateScope()) {
                TestExecutor exec = ServiceProvider.GetService<TestExecutor>();
                await exec.ExecuteAll(tests);
                return exec;
            }
        }

        private void EnsureAllTestsRegistered() =>
            TestFinder.FindTests(TestFinder.FindTestNames());
    }
}