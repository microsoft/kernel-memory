#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/.."

USR=kernelmemory
IMG=${USR}/service
TAG=:latest

set +e
docker rmi ${IMG}${TAG} >/dev/null 2>&1
set -e

if [ -z "$(docker images -q ${IMG}${TAG})" ]; then
  echo "All ${IMG}${TAG} local images have been deleted."
else
  echo "Some ${IMG}${TAG} local images are still present:"
  docker images ${IMG}${TAG}
  exit -1
fi

# See https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md#full-tag-listing
docker buildx build --no-cache --load \
    --platform=linux/amd64 \
    --build-arg BUILD_IMAGE_TAG=8.0-jammy-amd64 \
    --build-arg RUN_IMAGE_TAG=8.0-alpine-amd64 \
    -t ${IMG}${TAG} .

echo "Signing in as ${USR}..."
docker login -u ${USR}

echo "Pushing ${IMG}${TAG}..."
docker push "${IMG}${TAG}"
