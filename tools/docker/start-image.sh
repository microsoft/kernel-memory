#!/usr/bin/env bash

# Use this script to start the Docker image, mounting the current folder where a config file should exist

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

docker run -it --rm --name kernelmemory -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json kernel-memory/service:testing



# To inspect the image content
# Before starting it:
#   docker run -it --rm --name kernelmemory -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json --entrypoint /bin/bash kernel-memory/service:testing
# After starting it:
#   docker exec -it kernelmemory /bin/bash
