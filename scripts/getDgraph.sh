#!/bin/bash

# run me from the root project directory

nuget install Grpc.Tools -Version 1.7.1 -OutputDirectory artifacts

git clone --branch v1.0.4 --depth 1 https://github.com/dgraph-io/dgraph artifacts/dgraph

artifacts/Grpc.Tools.1.7.1/tools/macosx_x64/protoc -I artifacts/dgraph/protos \
    --csharp_out source/Dgraph-dotnet/DgraphAPI \
    --grpc_out source/Dgraph-dotnet/DgraphAPI artifacts/dgraph/protos/*.proto \
    --plugin=protoc-gen-grpc=artifacts/Grpc.Tools.1.7.1/tools/macosx_x64/grpc_csharp_plugin

