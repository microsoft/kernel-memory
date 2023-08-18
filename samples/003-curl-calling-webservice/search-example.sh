#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"

../../tools/search.sh -s http://127.0.0.1:9001 \
                      -q "Semantic Kernel" \
                      -f '"type":["test"]'

