#!/bin/bash
set -e

# Stand up a Dgraph instance with data stored in ./dgraph
# Assumes dgraph is installed separately.
# Run `dgraph-ratel` if you want to navigate to http://localhost:8000 to query data loaded by the examples. 


start() {
    if [ ! -d "dgraph" ]; then
        mkdir "dgraph"
    fi

    dgraph zero -w dgraph/wz > dgraph/zero.log 2>&1 &

    sleep 5s
    
    dgraph server -p dgraph/p -w dgraph/w --lru_mb 2048 --zero localhost:5080 > dgraph/server.log 2>&1 &
}

stop() {
    curl -s localhost:8080/admin/shutdown
    
    sleep 5s
    
    pkill "dgraph"

    echo ""
    echo "Shutdown complete"
}

clean() {
    rm -rf dgraph
}

if [ $# -gt 0 ]; then
    if [ $1 == 'start' ]; then
        start
    elif [ $1 == 'stop' ]; then
        stop
    elif [ $1 == 'clean' ]; then
        clean
    else 
        echo "start, stop or clean"
    fi
else 
    echo "start, stop or clean"
fi


