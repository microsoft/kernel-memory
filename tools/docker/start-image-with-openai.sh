#!/usr/bin/env bash

# Use this script to start the Docker image without a config file, with a basic setup using OpenAI default settings

if [ -z "$OPENAI_API_KEY" ]; then
    echo "The OPENAI_API_KEY environment variable is not set"
    exit 1
fi

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

# The logic handling $OPENAI_DEMO is in run.sh
docker run -it --rm --name kernelmemory -e OPENAI_DEMO="${OPENAI_API_KEY}" kernelmemory/service:latest
