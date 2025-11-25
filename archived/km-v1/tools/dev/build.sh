#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

clean() {
    dotnet clean --nologo -v q -c Release KernelMemory.sln
    dotnet clean --nologo -v q -c Debug KernelMemory.sln
}

echo "### Release build"
clean
dotnet build --nologo -v m -c Release KernelMemory.sln

echo "### Debug build"
clean
dotnet build --nologo -v m -c Debug KernelMemory.sln
