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
    cp .dockerignore   $ROOT/service/Service/out/docker/
    cp Dockerfile      $ROOT/service/Service/out/docker/
    cp run.sh          $ROOT/service/Service/out/docker/
}

build_docker_image() {
    echo "⏱️  Building Docker image..."
    cd $ROOT
    cd service/Service/out/docker/
    DOCKER_TAG="$DOCKER_IMAGE:testing"
    DOCKER_LABEL2="Commit=$(git log --pretty=format:'%H' -n 1)"
    DOCKER_LABEL3="Date=$(/usr/bin/env date +%Y-%m-%dT%H:%M:%S)"
    docker build --compress --tag $DOCKER_TAG --label "$DOCKER_LABEL2" --label "$DOCKER_LABEL3" .
}

echo "⏱️  Checking dependencies..."
check_dependency_dotnet
check_dependency_git
check_dependency_docker

cleanup_tmp_files
build_service
prepare_docker_image_src
build_docker_image
