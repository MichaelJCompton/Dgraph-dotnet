# Dgraph .Net C# library for Dgraph

This library for Dgraph with C# is distributed under the Apache 2.0 license.

It's developed independently to Dgraph.

Learn more about Dgraph at 

- [dgraph.io](https://dgraph.io/),
- [github](https://github.com/dgraph-io/dgraph), and
- [docs.dgraph.io](https://docs.dgraph.io/)

Versions of this library match up to Dgraph versions as follows:

| DgraphDotNet versions | Dgraph version |
| -------- | ------ |
| v0.1.0 | v1.0.3 |
| v0.2.0 | v1.0.4 |
| v0.3.0 | v1.0.5 |
| v0.4.0 .. v0.4.2 | v1.0.9 |

## Table of Contents
- [Getting](#obtaining-the-library)
- [Learning](#examples)
- [Using](#examples)
    * [Objects and JSON](#objects-and-json)
    * [Graph edges and mutations](#graph-edges-and-mutations)
    * [Edges in batches](#edges-in-batches)
- [Building](#building)
- [Contributing](#contributing)

## Getting

Grab the [Dgraph-dotnet](https://www.nuget.org/packages/Dgraph-dotnet/) NuGet package. 

## Learning

Checkout the examples in `source/Dgraph-dotnet.examples`.  There's a script in `source/Dgraph-dotnet.examples/scripts` to spin up a dgraph instance to run examples with.

## Using

There's three client interfaces.  

* `IDrgaphClient` for serialising objects to JSON and running queries 
* `IDgraphMutationsClient` for the above plus individual edge mutations
* `IDgraphBatchingClient` for the above plus batching updates

Upserts are supported by all three.

### Objects and JSON

Use your favourite JSON serialization library.

Have an object model

```c#
public class Person
{
    public string uid { get; set; }
    public string name { get; set; }
    public DateTime DOB { get; set; }
    public List<Person> friends { get; } = new List<Person>();
}
```

Make a new client

```c#
using(var client = DgraphDotNet.Clients.NewDgraphClient()) {
    client.Connect("127.0.0.1:9080");
```

Grab a transaction, serialize your object model to JSON, mutate the graph and commit the transaction.

```c#
    using(var transaction = client.NewTransaction()) {
        var json = ...serialize your object model...
        transaction.Mutate(json);
        transaction.Commit();
    }
```

Or to query the graph.

```c#
    using(var transaction = client.NewTransaction()) {
        var res = transaction.Query(query);
        
        dynamic newObjects = ...deserialize...(res.Value);

        ...
    }
```

Check out the example in `source/Dgraph-dotnet.examples/ObjectsToDgraph`.

### Graph edges and mutations

If you want to form mutations based on edge additions and deletions.

Make a mutations client giving it the address of the zero node.

```c#
using(IDgraphMutationsClient client = DgraphDotNet.Clients.NewDgraphMutationsClient("127.0.0.1:5080")) {
    client.Connect("127.0.0.1:9080");
```

Grab a transaction, add as many edge edges/properties to a mutation as required, submit the mutation, commit the transaction when done.

```c#
    using(var txn = client.NewTransactionWithMutations()) {
        var mutation = txn.NewMutation();
        var node = NewNode().Value;
        var property = Clients.BuildProperty(node, "someProperty", GraphValue.BuildStringValue("HI"));
        
        mutation.AddProperty(property.Value);
        var err = mutation.Submit();
        if(err.IsFailed) {
            // ... something went wrong
        }
        txn.Commit();
    }
    
```

Check out the example in `source/Dgraph-dotnet.examples/MutationExample`.


### Edges in batches

If you want to throw edges at Dgraph asynchronously, then add edges/properties to batches and the client handles the rest.

Make a batching client

```c#
using(IDgraphBatchingClient client = DgraphDotNet.Clients.NewDgraphBatchingClient("127.0.0.1:5080")) {
    client.Connect("127.0.0.1:9080");
```

Throw in edges

```c#
    var node = client.GetOrCreateNode("some-node");
    if (node.IsSuccess) {
        var property = Clients.BuildProperty(node.Value, "name", GraphValue.BuildStringValue("AName));
        if (property.IsSuccess) {
            client.BatchAddProperty(property.Value);
        }

        var edge = Clients.BuildEdge(node.Value, "friend", someOtherNode);  
        if (edge.IsSuccess) {
            client.BatchAddEdge(edge.Value);
        }

    }
``` 

No need to create or submit transactions; the client batches the edges up into transactions and submits to Dgraph asynchronously.

When done, flush out any remaning batches

```c#
    client.FlushBatches();
```                                                


Check out the example in `source/Dgraph-dotnet.examples/MovieLensBatch`.

### What client should I use?

Mostly, creating and using a `IDgraphClient` with `DgraphDotNet.Clients.NewDgraphClient()` and serializing an object model will be the right choice.

Use `IDgraphMutationsClient` or `IDgraphBatchingClient` if for example you are reading data from a file into a graph and don't want to build an object model client side, or are dealing with individual edges rather then an object model.

If you need to create nodes with unique identifying edges, then you'll need to use `Upsert()`.


## Building

To use the client, just include [Dgraph-dotnet](https://www.nuget.org/packages/Dgraph-dotnet/) NuGet package in you project.

To build, clone the repo and run the cake build (currently only working on bash) with `./build.sh`.  The required `.cs` files built from the Dgraph protos files aren't distributed with this source. The build process will clone the appropriate version of Dgraph and build the required `.cs` sources from the Dgraph protos into `source/Dgraph-dotnet/DgraphAPI`.  You can also just run `./scripts/getDgraph.sh` from the project root directory to clone dgraph and generate from protos, without building the Dgraph-dotnet library.  

## Contributing

Happy to take issues, suggestions and PRs.
