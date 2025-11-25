#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd $HERE

dotnet build -c Release --nologo -v m
