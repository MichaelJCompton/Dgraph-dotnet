# Dgraph .Net A C# library for Dgraph

This library for Dgraph with C# is distributed under the Apache 2.0 license.

Learn more about Dgraph at 

- [dgraph.io](https://dgraph.io/),
- [github](https://github.com/dgraph-io/dgraph), and
- [docs.dgraph.io](https://docs.dgraph.io/)

Versions of this library match up to Dgraph versions as follows:

| DgraphDotNet versions | Dgraph version |
| -------- | ------ |
| v0.1 - ... | v1.0.3 |

## Usage

### Objects and JSON

Use your favourite JSON serialization library.

Have an object model

```
public class Person
{
    public string UID { get; set; }
    public string name { get; set; }
    public DateTime DOB { get; set; }
    public List<Person> friends { get; } = new List<Person>();
}
```

Make a new client

```
using(var client = DgraphDotNet.Clients.NewDgraphClient()) {
    client.Connect("127.0.0.1:9080");
```

Grab a transaction, serialize your object model to JSON, mutate the graph and commit the transaction.

```
    using(var transaction = client.NewTransaction()) {
        var json = ...
        transaction.Mutate(json);
        transaction.Commit();
    }
```

Or to query the graph.

```
    using(var transaction = client.NewTransaction()) {
        var res = transaction.Query(query);
        
        dynamic newObjects = ...deserialize...(res.Value);

        ...
    }
```


### Graph edges and mutations

### Edges in batches

### What client should I use?

Mostly, creating and using an `IDgraphClient` with `DgraphDotNet.Clients.NewDgraphClient()` and serializing an object model will be the right choice.

Use `IDgraphMutationsClient` or `IDgraphBatchingClient` if you are reading data from a file into a graph and don't want to build an object model client side: for example, reading and storing data from C# but using the data as JSON from javasscript.

If you need to create nodes with unique identifying edges, then you might need to upsert and so the mutations client might be required.
