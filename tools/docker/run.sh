#!/usr/bin/env bash

# This script is copied into the Docker image and is used to start Kernel Memory service

set -e

cd /app/

CFG1="/app/data/appsettings.Development.json"
CFG2="/app/data/appsettings.development.json"
CFG3="/app/data/appsettings.Production.json"
CFG4="/app/data/appsettings.production.json"
CFG5="/app/data/appsettings.json"
CFG="notfound"

if [ -e "$CFG1" ]; then
    CFG=$CFG1
elif [ -e "$CFG2" ]; then
    CFG=$CFG2
elif [ -e "$CFG3" ]; then
    CFG=$CFG3
elif [ -e "$CFG4" ]; then
    CFG=$CFG4
elif [ -e "$CFG5" ]; then
    CFG=$CFG5
fi

if [ ! -e "$CFG" ]; then
    echo -e "\nERROR: Configuration file not found.\n"
    echo -e "Please mount a volume containing either appsettings.development.json, appsettings.production.json or appsettings.json. \n"
    echo -e "=> docker run --volume <path>/appsettings.Development.json:/app/data/appsettings.json .....\n"
    echo -e "\nExample:\n"
    echo "=> docker run --volume ./service/Service/appsettings.Development.json:/app/data/appsettings.json -it --rm --name kernelmemory kernel-memory/service:testing"
    echo -e "\n"
    exit 1
fi

cp "$CFG" appsettings.Production.json

export ASPNETCORE_ENVIRONMENT=Production

dotnet Microsoft.KernelMemory.ServiceAssembly.dll
