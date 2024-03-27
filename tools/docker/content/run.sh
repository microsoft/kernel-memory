#!/usr/bin/env sh

# This script is copied into the Docker image and is used to start Kernel Memory service

set -e

cd /app/

# If OPENAI_DEMO is set, handle special case for demos
if [ -n "$OPENAI_DEMO" ]; then
  export SKIP_CFG_CHECK=1
  # Note about config and env vars:
  # * KernelMemory.Services.OpenAI.APIKey                   => KernelMemory__Services__OpenAI__APIKey
  # * KernelMemory.DataIngestion.EmbeddingGeneratorTypes[0] => KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0
  # etc.
  export KernelMemory__Services__OpenAI__APIKey="${OPENAI_DEMO}"
  export KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0=OpenAI
  export KernelMemory__TextGeneratorType=OpenAI
  export KernelMemory__Retrieval__EmbeddingGeneratorType=OpenAI
fi

# If SKIP_CFG_CHECK is not set, search for a config file
if [ -z "$SKIP_CFG_CHECK" ]; then
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
        echo "=> docker run --volume ./service/Service/appsettings.Development.json:/app/data/appsettings.json -it --rm --name kernelmemory kernelmemory/service:latest"
        echo -e "\n"
        echo -e "You can also set config values using env vars, for example:\n"
        echo "=> docker run -it --rm --name kernelmemory -e SKIP_CFG_CHECK=1        \\"
        echo "    -e KernelMemory__Services__OpenAI__APIKey=\"[API KEY]\"             \\"
        echo "    -e KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0=OpenAI \\"
        echo "    -e KernelMemory__TextGeneratorType=OpenAI                         \\"
        echo "    -e KernelMemory__Retrieval__EmbeddingGeneratorType=OpenAI         \\"
        echo "    kernelmemory/service:latest"
        exit 1
    fi
    
    cp "$CFG" appsettings.Production.json
fi

export ASPNETCORE_ENVIRONMENT=Production

dotnet Microsoft.KernelMemory.ServiceAssembly.dll
