#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$ROOT"
TMPDIR="$ROOT/.tmp"
mkdir -p "$TMPDIR"
export TMPDIR

dotnet format src/Core/Core.csproj
dotnet format src/Main/Main.csproj
dotnet format tests/Core.Tests/Core.Tests.csproj
dotnet format tests/Main.Tests/Main.Tests.csproj
