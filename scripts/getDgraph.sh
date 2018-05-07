#!/bin/bash

# run me from the root project directory

nuget install Grpc.Tools -Version 1.7.1 -OutputDirectory artifacts

git clone --branch v1.0.5 --depth 1 https://github.com/dgraph-io/dgraph artifacts/dgraph
git clone --branch master --depth 1 https://github.com/dgraph-io/dgo artifacts/dgo

echo "Building C# protos files"

artifacts/Grpc.Tools.1.7.1/tools/macosx_x64/protoc -I artifacts/dgraph/protos -I artifacts/dgo/protos \
    --csharp_out source/Dgraph-dotnet/DgraphAPI \
    --grpc_out source/Dgraph-dotnet/DgraphAPI artifacts/dgraph/protos/*.proto artifacts/dgo/protos/*.proto \
    --plugin=protoc-gen-grpc=artifacts/Grpc.Tools.1.7.1/tools/maco
