#!/usr/bin/env bash

# Use this script to avoid relying on KM published packages,
# and use the local source code instead, e.g. in case changes
# are being made to Abstractions, Core, etc.

set -e

cd "$(dirname "${BASH_SOURCE[0]:-$0}")"
cd ../service/Service

dotnet clean

# Build using a different solution name. The same can be done using a KernelMemoryDev.sln file.
# Note: dotnet run doesn't support [ -p "SolutionName=KernelMemoryDev" ]
dotnet build -c Debug -p "SolutionName=KernelMemoryDev"

# Run the special build, detached from external KM nugets.
dotnet run -c Debug --no-build --no-restore
