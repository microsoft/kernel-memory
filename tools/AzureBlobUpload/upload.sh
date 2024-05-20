#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $HERE

# if file x doesn't exist, then create it
if [ ! -f "bin/Release/net8.0/AzureBlobUpload.dll" ]; then
    echo "Building tool..."
    dotnet build -c Release --nologo -v q
fi

dotnet run -c Release --no-build $*
