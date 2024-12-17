#!/usr/bin/env bash

set -e

cd "$(dirname "${BASH_SOURCE[0]:-$0}")"

ASPNETCORE_ENVIRONMENT=Development dotnet run
