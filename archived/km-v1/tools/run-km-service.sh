#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd .. && pwd)"
cd $ROOT

cd service/Service

dotnet clean
dotnet build -c Debug
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --no-restore
