#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"

dotnet restore
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run

