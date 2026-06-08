#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $ROOT

rm -rf TestResults

rm -rf src/Core/bin
rm -rf src/Core/obj

rm -rf src/Main/bin
rm -rf src/Main/obj

rm -rf tests/Core.Tests/bin
rm -rf tests/Core.Tests/obj
rm -rf tests/Core.Tests/TestResults

rm -rf tests/Main.Tests/bin
rm -rf tests/Main.Tests/obj
rm -rf tests/Main.Tests/TestResults
