#!/usr/bin/env bash

set -e

DOCKER_IMAGE="kernel-memory/service"
CONFIGURATION=Release

# Change current dir to repo root
HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

check_dependency_dotnet() {
    set +e
    TEST=$(which dotnet)
    if [[ -z "$TEST" ]]; then
        echo "🔥 ERROR: 'dotnet' command not found."
        echo "Install .NET Core and make sure the 'dotnet' command is in the PATH."
        echo ".NET Core installation: https://dotnet.github.io"
        exit 1
    fi
    set -e
}

check_dependency_git() {
    set +e
    TEST=$(which git)
    if [[ -z "$TEST" ]]; then
        echo "🔥 ERROR: 'git' command not found."
        echo "Install git CLI and make sure the 'git' command is in the PATH."
        exit 1
    fi
    set -e
}

check_dependency_docker() {
    set +e
    TEST=$(which docker)
    if [[ -z "$TEST" ]]; then
        echo "🔥 ERROR: 'docker' command not found."
        echo "Install docker CLI and make sure the 'docker' command is in the PATH."
        exit 1
    fi
    set -e
}

cleanup_tmp_files() {
    echo "⏱️  Cleaning up..."
    cd $ROOT
    cd service/Service
    rm -fR bin obj out
}

build_service() {
    cd $ROOT
    cd service/Service
    echo "⏱️  Restoring .NET packages..."
    dotnet restore
    echo "⏱️  Building .NET app..."
    dotnet build --configuration $CONFIGURATION
}

prepare_docker_image_src() {
    cd $ROOT
    cd service/Service
    echo "⏱️  Publishing .NET build..."
    dotnet publish --configuration $CONFIGURATION --output out/docker
    cd $HERE
    cp Dockerfile      $ROOT/service/Service/out/docker/
    cp .dockerignore   $ROOT/service/Service/out/docker/
    cp content/run.sh  $ROOT/service/Service/out/docker/
}

build_docker_image() {
    echo "⏱️  Building Docker image..."
    cd $ROOT
    cd service/Service/out/docker/
    SHORT_COMMIT_ID=$(git rev-parse --short HEAD)
    LONG_COMMIT_ID=$(git rev-parse HEAD)
    SHORT_DATE=$(/usr/bin/env date +%Y%m%d)
    LONG_DATE=$(/usr/bin/env date +%Y-%m-%dT%H:%M:%S)
    DOCKER_TAG1="${DOCKER_IMAGE}:${SHORT_DATE}_${SHORT_COMMIT_ID}"
    DOCKER_TAG2="${DOCKER_IMAGE}:latest"
    DOCKER_LABEL1="Commit=${LONG_COMMIT_ID}"
    DOCKER_LABEL2="Date=${LONG_DATE}"
    docker build --compress --tag "$DOCKER_TAG1" --tag "$DOCKER_TAG2" --label "$DOCKER_LABEL1" --label "$DOCKER_LABEL2" .
    
    echo -e "\n\n✅  Docker images ready:"
    echo -e " - $DOCKER_TAG1"
    echo -e " - $DOCKER_TAG2"
}

howto_test() {
  echo -e "\nTo test the image with OpenAI:\n"
  echo "  docker run -it --rm -e OPENAI_DEMO=\"...OPENAI API KEY...\" kernel-memory/service"
  
  echo -e "\nTo test the image with your local config:\n"
  echo "  docker run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json kernel-memory/service"
  
  echo -e "\nTo inspect the image content:\n"
  echo "  docker run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json --entrypoint /bin/sh kernel-memory/service"
  
  echo ""
}

echo "⏱️  Checking dependencies..."
check_dependency_dotnet
check_dependency_git
check_dependency_docker

cleanup_tmp_files
build_service
prepare_docker_image_src
build_docker_image
howto_test
