#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

dotnet test KernelMemory.sln -c Debug --nologo --filter Category=UnitTest -v q
