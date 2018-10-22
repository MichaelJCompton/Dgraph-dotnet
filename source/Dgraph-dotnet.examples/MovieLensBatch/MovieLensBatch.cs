/** 
 * Example of using Dgraph client with a batching client.  This is based on an
 * example I wrote for Dgraph version 0.8
 * https://github.com/dgraph-io/dgraph/tree/release/v0.8.0/wiki/resources/examples/goclient/movielensbatch
 *
 * Data comes from http://grouplens.org/datasets/movielens/100k/
 * http://files.grouplens.org/datasets/movielens/ml-100k.zip
 *
 * Running :
 *  1) run the script in ./data to download data 
 *  1) start up dgraph (e.g. see ../scripts/server.sh)
 *  2) dotnet run
 *
 */



using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DgraphDotNet;
using DgraphDotNet.Graph;
using FluentResults;

namespace BatchExample {
    class MovielensBatch {
        private static string path = "./data/";
        private static string schemaFile = path + "movielens.schema";
        private static string genreFile = path + "ml-100k/u.genre";
        private static string userFile = path + "ml-100k/u.user";
        private static string movieFile = path + "ml-100k/u.item";
        private static string ratingFile = path + "ml-100k/u.data";

        private long numProcessed;

        static void Main(string[] args) {
            var loader = new MovielensBatch();
            loader.LoadMovieLensData();
        }

        public void LoadMovieLensData() {
            try {
                using(IDgraphBatchingClient client = DgraphDotNet.Clients.NewDgraphBatchingClient("127.0.0.1:5080")) {
                    client.Connect("127.0.0.1:9080");

                    var version = client.CheckVersion();
                    if(version.IsSuccess) {
                        Console.WriteLine($"Connected to Dgraph (version {version.Value})");
                    } else {
                        Console.WriteLine($"Unable to read Dgraph version ({version})");
                    }

                    var schema = System.IO.File.ReadAllText(schemaFile);
                    client.AlterSchema(schema);

                    // How to query schema
                    var result = client.SchemaQuery("schema { }");
                    if(result.IsFailed) {
                        Console.WriteLine("Something went wrong : " + result);
                        System.Environment.Exit(1);
                    }
                    Console.WriteLine("schema is : ");
                    foreach(var predicate in result.Value) {
                        Console.WriteLine(predicate);
                    }

                    var cancelToken = new CancellationTokenSource();
                    var ticker = RunTicker(cancelToken.Token);

                    // Read all files in parallel.
                    //
                    // Client correctly creates and links the nodes, no matter
                    // what order they are read in.
                    Task.WaitAll(
                        ProcessGenres(client),
                        ProcessUsers(client),
                        ProcessMovies(client),
                        ProcessRatings(client));

                    cancelToken.Cancel();
                    Task.WaitAll(ticker);

                    client.FlushBatches();
                }

                Console.WriteLine("All files processed.");
                Console.WriteLine(numProcessed + " lines read from files."); 
            } catch (Exception e) {
                Console.WriteLine("Error creating database");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private Task ProcessGenres(IDgraphBatchingClient client) {
            return Task.Run(() => {
                // Each line in genre file looks like
                //
                // genre-name|genreID
                //
                // We'll use a client-side node named "genre<genreID>" to identify each genre node.
                // That name isn't persisted in the store in this example, it's just for client-side reference.
                using(FileStream fs = new FileStream(genreFile, FileMode.Open)) {
                    using(StreamReader genres = new StreamReader(fs)) {
                        string line;

                        while ((line = genres.ReadLine()) != null) {
                            Interlocked.Increment(ref numProcessed);

                            var split = line.Split(new char[] { '|' });

                            if (split.Length == 2) {
                                var node = client.GetOrCreateNode("genre" + split[1]);
                                if (node.IsSuccess) {
                                    var edge = Clients.BuildProperty(node.Value, "name", GraphValue.BuildStringValue(split[0]));
                                    if (edge.IsSuccess) {
                                        client.BatchAddProperty(edge.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private Task ProcessUsers(IDgraphBatchingClient client) {
            return Task.Run(() => {
                // Each line in the user file looks like
                //
                // userID|age|genre|occupation|ZIPcode
                //
                // We'll use a node named "user<userID>" to identify each user node
                using(FileStream fs = new FileStream(userFile, FileMode.Open)) {
                    using(StreamReader users = new StreamReader(fs)) {
                        string line;

                        while ((line = users.ReadLine()) != null) {
                            Interlocked.Increment(ref numProcessed);

                            var split = line.Split(new char[] { '|' });

                            if (split.Length == 5 && long.TryParse(split[1], out long age)) {
                                var node = client.GetOrCreateNode("user" + split[0]);
                                if (node.IsSuccess) {
                                    var ageEdge = Clients.BuildProperty(node.Value, "age", GraphValue.BuildIntValue(age));
                                    var gender = Clients.BuildProperty(node.Value, "gender", GraphValue.BuildStringValue(split[2]));
                                    var occupation = Clients.BuildProperty(node.Value, "occupation", GraphValue.BuildStringValue(split[3]));
                                    var zipcode = Clients.BuildProperty(node.Value, "zipcode", GraphValue.BuildStringValue(split[4]));
                                    if (ageEdge.IsSuccess && gender.IsSuccess && occupation.IsSuccess && zipcode.IsSuccess) {
                                        client.BatchAddProperty(ageEdge.Value);
                                        client.BatchAddProperty(gender.Value);
                                        client.BatchAddProperty(occupation.Value);
                                        client.BatchAddProperty(zipcode.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private Task ProcessMovies(IDgraphBatchingClient client) {
            return Task.Run(() => {
                // The lines of the movie file look like
                //
                // movieID|movie-name|date||imdb-address|genre0?|genre1?|...|genre18?
                //
                // We'll use "movie<movieID>" as the node name
                using(FileStream fs = new FileStream(movieFile, FileMode.Open)) {
                    using(StreamReader movies = new StreamReader(fs)) {
                        string line;

                        while ((line = movies.ReadLine()) != null) {
                            Interlocked.Increment(ref numProcessed);

                            var split = line.Split(new char[] { '|' });

                            if (split.Length == 24) {
                                var movieNode = client.GetOrCreateNode("movie" + split[0]);
                                if (movieNode.IsSuccess) {
                                    var name = Clients.BuildProperty(movieNode.Value, "name", GraphValue.BuildStringValue(split[1]));
                                    if (name.IsSuccess) {
                                        client.BatchAddProperty(name.Value);

                                        // 1 in the column means the movie has the corresponding genre
                                        for (int i = 5; i < 24; i++) {
                                            if (split[i] == "1") {
                                                var genreNode = client.GetOrCreateNode("genre" + (i - 5));
                                                if (genreNode.IsSuccess) {
                                                    var genre = Clients.BuildEdge(movieNode.Value, "genre", genreNode.Value);
                                                    if (genre.IsSuccess) {
                                                        client.BatchAddEdge(genre.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private Task ProcessRatings(IDgraphBatchingClient client) {
            return Task.Run(() => {
                // Each line in the rating file looks like
                //
                // userID     movieID     rating       timestamp
                using(FileStream fs = new FileStream(ratingFile, FileMode.Open)) {
                    using(StreamReader ratings = new StreamReader(fs)) {
                        string line;

                        while ((line = ratings.ReadLine()) != null) {
                            Interlocked.Increment(ref numProcessed);

                            var split = line.Split(new char[] { '\t' });

                            if (split.Length == 4) {
                                var userNode = client.GetOrCreateNode("user" + split[0]);
                                var movieNode = client.GetOrCreateNode("movie" + split[1]);
                                if (userNode.IsSuccess && movieNode.IsSuccess) {
                                    Dictionary<string, string> facets = new Dictionary<string, string>();
                                    facets.Add("rating", split[2]);
                                    var rated = Clients.BuildEdge(userNode.Value, "rated", movieNode.Value, facets);
                                    if (rated.IsSuccess) {
                                        client.BatchAddEdge(rated.Value);
                                    }
                                }
                            }
                        }
                    }
                }

            });
        }

        private Task RunTicker(CancellationToken cancelToken) {
            return Task.Run(() => {
                char[] tickCodes = { '|', '/', '-', '\\' };
                int currentTick = 0;
                long lastSeen = 0;

                Console.Write("Processing files :  ");

                while (true) {
                    var cur = Interlocked.Read(ref numProcessed);
                    if (cur - lastSeen > 100) {
                        currentTick = (currentTick + 1) % tickCodes.Length;
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(tickCodes[currentTick]);
                        lastSeen = cur;
                    }

                    if (cancelToken.IsCancellationRequested) {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.WriteLine();
                        return;
                    }
                }
            });
        }

    }
}