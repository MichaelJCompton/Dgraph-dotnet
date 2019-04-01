# End-to-end tests for Dgraph-dotnet.

You can view this set of tests as the contract Dgraph-dotnet is keeping about it's api and interaction with Dgraph.  Changes to the expected results or the calls are braking changes for Dgraph-dotnet.

This project does these things:

- Tests that a Dgraph-dotnet version works correctly with a given Dgraph version: e.g. soon the versions of Dgraph particular Dgraph-dotnet versions work with, will be based on automated test runs against Dgraph versions.
- Gives a set of end-to-end examples of how to use Dgraph-dotnet.  Because these examples and their output are tested on every build, they a more reliable and up-to-date than other examples.  I'll typically add examples here (and transition existing example to here) rather than maintaining external examples that aren't guaranteed to work with particular versions.
- Shows how GraphSchema.io can spin up and tear down Dgraph instances; e.g. in a dev or testing environment.


