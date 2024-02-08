#!/usr/bin/env sh

# This script is copied into the Docker image and is used to start Kernel Memory service configuration wizard

set -e

cd /app/

dotnet Microsoft.KernelMemory.ServiceAssembly.dll setup
