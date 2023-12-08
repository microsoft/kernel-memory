#!/usr/bin/env bash

set -e

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd ../service/Service

dotnet restore
dotnet build
dotnet run setup

