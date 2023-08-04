#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"

../../tools/upload-file.sh -f test.pdf \
                           -s http://127.0.0.1:9001 \
                           -u curlUser \
                           -t "type=test" \
                           -i curlExample01
