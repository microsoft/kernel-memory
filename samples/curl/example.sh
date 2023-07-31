#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"

../../curl/upload-file.sh -f test.pdf \
                          -s http://127.0.0.1:9001/upload \
                          -u curlUser \
                          -t "type=test" \
                          -i curlExample01