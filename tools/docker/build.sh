#!/usr/bin/env bash

set -e

DOCKER_IMAGE="kernelmemory/service"

# Change current dir to repo root
HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

# Check if Docker is installed
check_dependency_docker() {
    set +e
    TEST=$(which docker)
    if [[ -z "$TEST" ]]; then
        echo "🔥 ERROR: 'docker' command not found."
        echo "Install Docker CLI and make sure the 'docker' command is in the PATH."
        exit 1
    fi
    set -e
}

# Generate a random string
uuid()
{
    local N B T
    for (( N=0; N < 16; ++N ))
    do
        B=$(( $RANDOM%255 ))
        if (( N == 6 ))
        then
            printf '4%x' $(( B%15 ))
        elif (( N == 8 ))
        then
            local C='89ab'
            printf '%c%x' ${C:$(( $RANDOM%${#C} )):1} $(( B%15 ))
        else
            printf '%02x' $B
        fi
    done
    echo
}

# Build docker image and add tags
build_docker_image() {
    echo "⏱️  Building Docker image..."
    cd $HERE
    DOCKER_TAG1="${DOCKER_IMAGE}:latest"
    DOCKER_TAGU="${DOCKER_IMAGE}:$(uuid)"
    
    #docker build --compress --tag "$DOCKER_TAG1" --tag "$DOCKER_TAGU" \
    #  --build-arg="SOURCE=https://github.com/.../kernel-memory" \
    #  --build-arg="BRANCH=..." .
    
    docker build --compress --tag "$DOCKER_TAG1" --tag "$DOCKER_TAGU" .
    
    # Read versions details (removing \r char)
    SHORT_DATE=$(docker run -it --rm -a stdout --entrypoint cat "$DOCKER_TAGU" .SHORT_DATE)
    SHORT_DATE="${SHORT_DATE%%[[:cntrl:]]}"
    SHORT_COMMIT_ID=$(docker run -it --rm -a stdout --entrypoint cat "$DOCKER_TAGU" .SHORT_COMMIT_ID)
    SHORT_COMMIT_ID="${SHORT_COMMIT_ID%%[[:cntrl:]]}"
    
    # Add version tag
    DOCKER_TAG2="${DOCKER_IMAGE}:${SHORT_DATE}.${SHORT_COMMIT_ID}"
    docker tag $DOCKER_TAGU $DOCKER_TAG2
    docker rmi $DOCKER_TAGU
    
    echo -e "\n\n✅  Docker image ready:"
    echo -e " - $DOCKER_TAG1"
    echo -e " - $DOCKER_TAG2"
}

# Print some instructions
howto_test() {
  echo -e "\nTo test the image with OpenAI:\n"
  echo "  docker run -it --rm -e OPENAI_DEMO=\"...OPENAI API KEY...\" kernelmemory/service"
  
  echo -e "\nTo test the image with your local config:\n"
  echo "  docker run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json kernelmemory/service"
  
  echo -e "\nTo inspect the image content:\n"
  echo "  docker run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json --entrypoint /bin/sh kernelmemory/service"
  
  echo ""
}

echo "⏱️  Checking dependencies..."
check_dependency_docker

build_docker_image
howto_test
