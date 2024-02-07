#!/usr/bin/env bash

set -e

DOCKER_IMAGE=${DOCKER_IMAGE:-"kernelmemory/service"}

# Change current dir to repo root
HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

# Check if Docker or compatible CLI is installed
set_docker_cli() {
    set +e
    # check which CLI is installed
    if [[ -n "$(which docker 2> /dev/null)" ]]; then
        echo "Using 'docker' to build the image."
        DOCKER_EXEC=docker
    elif [[ -n "$(which podman 2> /dev/null)" ]]; then
        echo "Using 'podman' to build the image."
        DOCKER_EXEC=podman
    elif [[ -n "$(which nerdctl 2> /dev/null)" ]]; then
        echo "Using 'nerdctl' to build the image."
        DOCKER_EXEC=nerdctl
    else
        echo "🔥 ERROR: No Docker compatible command was found."
        echo "Install Docker compatible CLI (docker, podman or nerdctl) and make sure the command is in the PATH."
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
    
    #$DOCKER_EXEC build --compress --tag "$DOCKER_TAG1" --tag "$DOCKER_TAGU" \
    #  --build-arg="SOURCE=https://github.com/.../kernel-memory" \
    #  --build-arg="BRANCH=..." .
    
    $DOCKER_EXEC build --compress --tag "$DOCKER_TAG1" --tag "$DOCKER_TAGU" .
    
    # Read versions details (removing \r char)
    SHORT_DATE=$($DOCKER_EXEC run -it --rm -a stdout --entrypoint cat "$DOCKER_TAGU" .SHORT_DATE)
    SHORT_DATE="${SHORT_DATE%%[[:cntrl:]]}"
    SHORT_COMMIT_ID=$($DOCKER_EXEC run -it --rm -a stdout --entrypoint cat "$DOCKER_TAGU" .SHORT_COMMIT_ID)
    SHORT_COMMIT_ID="${SHORT_COMMIT_ID%%[[:cntrl:]]}"
    
    # Add version tag
    DOCKER_TAG2="${DOCKER_IMAGE}:${SHORT_DATE}.${SHORT_COMMIT_ID}"
    $DOCKER_EXEC tag $DOCKER_TAGU $DOCKER_TAG2
    $DOCKER_EXEC rmi $DOCKER_TAGU
    
    echo -e "\n\n✅  Docker image ready:"
    echo -e " - $DOCKER_TAG1"
    echo -e " - $DOCKER_TAG2"
}

# Print some instructions
howto_test() {
  echo -e "\nTo test the image with OpenAI:\n"
  echo "  $DOCKER_EXEC run -it --rm -e OPENAI_DEMO=\"...OPENAI API KEY...\" kernelmemory/service"
  
  echo -e "\nTo test the image with your local config:\n"
  echo "  $DOCKER_EXEC run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json kernelmemory/service"
  
  echo -e "\nTo inspect the image content:\n"
  echo "  $DOCKER_EXEC run -it --rm -v ./service/Service/appsettings.Development.json:/app/data/appsettings.json --entrypoint /bin/sh kernelmemory/service"
  
  echo ""
}

echo "⏱️  Checking dependencies..."
set_docker_cli

build_docker_image
howto_test
