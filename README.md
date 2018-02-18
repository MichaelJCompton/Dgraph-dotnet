# Dgraph .Net A C# library for Dgraph

Learn more about Dgraph at 

- [dgraph.io](https://dgraph.io/),
- [github](https://github.com/dgraph-io/dgraph), and
- [docs.dgraph.io](https://docs.dgraph.io/)

| DgraphDotNet versions | Dgraph version |
| -------- | ------ |
| v0.1 - ... | v1.0.3 |

## Usage

### Objects and JSON

Using your favourite JSON serialization library.


...quick example here and ref to examples in code


### Graph edges and mutations

### Edges in batches

### What should I use?

Mostly creating and using an `IDgraphClient` and serializing a object model will be the right choice.

Use `IDgraphMutationsClient` or `IDgraphBatchingClient` if you are reading data from a file into a graph and don't want to build an object model client side: for example, reading and storing data from C# but using the data as JSON from javasscript.
