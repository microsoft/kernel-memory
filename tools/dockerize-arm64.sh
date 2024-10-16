#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/.."

USR=kernelmemory
IMG=${USR}/service
TAG1=:latest-arm64

# Prompt user for TAG2
read -p "Enter TAG2 (e.g. '0.99.250214.1-arm64'): " TAG2

# Ensure TAG2 starts with ':' and ends with '-arm64'
if [[ "${TAG2:0:1}" != ":" ]]; then
  TAG2=":${TAG2}"
fi

if [[ "${TAG2:(-6)}" != "-arm64" ]]; then
  TAG2="${TAG2}-arm64"
fi

set +e
docker rmi ${IMG}${TAG1} >/dev/null 2>&1
docker rmi ${IMG}${TAG2} >/dev/null 2>&1
set -e

# Remove images if they exist
for IMAGE_TAG in "${TAG1}" "${TAG2}"; do
  if docker rmi "${IMG}${IMAGE_TAG}" >/dev/null 2>&1; then
    echo "Removed local image ${IMG}${IMAGE_TAG}"
  else
    echo "Image ${IMG}${IMAGE_TAG} was not found or failed to be removed."
  fi
done

# Check that all images have been removed
for IMAGE_TAG in "${TAG1}" "${TAG2}"; do
  if [ -z "$(docker images -q "${IMG}${IMAGE_TAG}")" ]; then
    echo "All ${IMG}${IMAGE_TAG} local images have been deleted."
  else
    echo "Some ${IMG}${IMAGE_TAG} local images are still present:"
    docker images "${IMG}${IMAGE_TAG}"
    exit 1
  fi
done

# See https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md#full-tag-listing
docker buildx build --no-cache --load \
    --platform=linux/arm64 \
    --build-arg BUILD_IMAGE_TAG=8.0-jammy-arm64v8 \
    --build-arg RUN_IMAGE_TAG=8.0-alpine-arm64v8 \
    -t ${IMG}${TAG1} -t ${IMG}${TAG2} \
    .

echo "Signing in as ${USR}..."
docker login -u ${USR}

# Push images to Docker registry
for IMAGE_TAG in "${TAG1}" "${TAG2}"; do
  echo "Pushing ${IMG}${IMAGE_TAG}..."
  if ! docker push "${IMG}${IMAGE_TAG}"; then
    echo "Failed to push ${IMG}${IMAGE_TAG}."
    exit 1
  fi
done

echo "Docker image push complete."
