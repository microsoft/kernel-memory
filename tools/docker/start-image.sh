#!/usr/bin/env bash

# Use this script to start the Docker image, mounting the current folder where a config file should exist

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

docker run -it --rm --name kernelmemory -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json kernelmemory/service



# To inspect the image content after starting it: docker exec -it kernelmemory /bin/sh
# ... or before starting it: docker run -it --rm --name kernelmemory -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json --entrypoint /bin/sh kernelmemory/service:latest


