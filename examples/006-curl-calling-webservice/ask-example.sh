#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"

../../tools/ask.sh -s http://127.0.0.1:9001 \
                   -q "tell me about Semantic Kernel" \
                   -f '"type":["test"]'

