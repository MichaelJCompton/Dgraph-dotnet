# Dgraph .Net C# library for Dgraph

Official Dgraph client implementation for C#. Distributed under the Apache 2.0 license.

Before using this client, we highly recommend that you go through [docs.dgraph.io],
and understand how to run and work with Dgraph.

[docs.dgraph.io]:https://docs.dgraph.io

## Table of Contents
- [Install](#install)
- [Supported Versions](#supported-versions) 
- [Learning](#learning)
- [Using](#using)
    * [Objects and JSON](#objects-and-json)
    * [Graph edges and mutations](#graph-edges-and-mutations)
    * [Edges in batches](#edges-in-batches)
- [Building](#building)
- [Contributing](#contributing)

## Install

Grab the [Dgraph-dotnet](https://www.nuget.org/packages/Dgraph-dotnet/) NuGet package. 

## Supported Versions

Versions of this library match up to Dgraph versions as follows:

| DgraphDotNet versions | Dgraph version |
| -------- | ------ |
| v0.7.0 | v1.1 |
| v0.6.0 | v1.0.14, v1.0.13 |
| v0.5.0 .. v0.5.3 | v1.0.13 |
| v0.4.0 .. v0.4.2 | v1.0.9 |
| v0.3.0 | v1.0.5 |
| v0.2.0 | v1.0.4 |
| v0.1.0 | v1.0.3 |

Checkout the [Changelog](https://github.com/MichaelJCompton/Dgraph-dotnet/blob/master/Changelog.md) for changes between versions.

## Learning

*The examples are being replaced by automated end-to-end testing that shows how to use the library and gets run on each build against compatible Dgraph versions.  The testing is being built out in source/Dgraph-dotnet.tests.e2e.  The examples projects will be removed as the functionaly they show is tested in the end-to-end tests.*

Checkout the examples in `source/Dgraph-dotnet.examples`.  There's a script in `source/Dgraph-dotnet.examples/scripts` to spin up a dgraph instance to run examples with.

## Using

There's three client interfaces.  

* `IDrgaphClient` for serialising objects to JSON and running queries 
* `IDgraphMutationsClient` for the above plus individual edge mutations
* `IDgraphBatchingClient` for the above plus batching updates

Upserts are supported by all three.

Communication with Dgraph is via grpc.  Because that's naturally asynchronous, practically everything in the library is `async`.

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
        await transaction.Mutate(json);
        await transaction.Commit();
    }
```

Or to query the graph.

```c#
    using(var transaction = client.NewTransaction()) {
        var res = await transaction.Query(query);
        
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
        var err = await mutation.Submit();
        if(err.IsFailed) {
            // ... something went wrong
        }
        await txn.Commit();
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
            await client.BatchAddProperty(property.Value);
        }

        var edge = Clients.BuildEdge(node.Value, "friend", someOtherNode);  
        if (edge.IsSuccess) {
            await client.BatchAddEdge(edge.Value);
        }

    }
``` 

No need to create or submit transactions; the client batches the edges up into transactions and submits to Dgraph asynchronously.

When done, flush out any remaning batches

```c#
    await client.FlushBatches();
```                                                


Check out the example in `source/Dgraph-dotnet.examples/MovieLensBatch`.

### What client should I use?

Mostly, creating and using a `IDgraphClient` with `DgraphDotNet.Clients.NewDgraphClient()` and serializing an object model will be the right choice.

Use `IDgraphMutationsClient` or `IDgraphBatchingClient` if for example you are reading data from a file into a graph and don't want to build an object model client side, or are dealing with individual edges rather then an object model.

If you need to create nodes with unique identifying edges, then you'll need to use `Upsert()`.


## Building

To use the client, just include [Dgraph-dotnet](https://www.nuget.org/packages/Dgraph-dotnet/) NuGet package in you project.

To build from source, just run `dotnet build`, `dotnet test`, etc.

## Contributing

Happy to take issues, suggestions and PRs.
