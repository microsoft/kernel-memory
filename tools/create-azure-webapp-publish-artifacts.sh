#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE"

cd ../dotnet/Service

dotnet publish -c Release -o ./bin/Publish
