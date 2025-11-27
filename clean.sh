#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $ROOT

rm -rf TestResults

rm -rf src/Core/bin
rm -rf src/Core/obj

rm -rf src/Main/bin
rm -rf src/Main/obj

rm -rf tests/Core.Tests/TestResults
