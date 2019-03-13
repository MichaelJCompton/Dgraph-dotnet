This is a bit naf.  I had a nice auto building pipeline that pull dgraph and built all the .proto dependencies in the build script.  But with either (or both) of the changes to how Dgraph imports protos from dgo and badger, or cause of how grpc is now built using the .csproj, I can't get it to work. (it wasn't 100% anyway cause there aren't releases of dgo to pull that match the dgraph releases)

Best I can do at the moment is to include the current versions of the proto files in this repo.

On each version bump I'll have to:

- make sure I have the right versions of dgraph, dgo and badger protos
- rename the badger one to badger.proto
- edit github.com/dgraph-io/dgraph/protos/pb.proto so that...

```
import "github.com/dgraph-io/dgo/protos/api.proto";
import "github.com/dgraph-io/badger/pb/badger.proto";
```

I should be able to get this right, but for the moment, this will have to do.  I'm changing from cake builds to gcp cloud build anyway, so I might be able to get something nicer with that change in a version soon.