#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/../.."

USR=kernelmemory
IMG=${USR}/service

# Prompt user for VERSION
read -p "Enter VERSION (e.g. '0.99.260214.1'): " VERSION

# Ensure VERSION ends with arch name
if [[ "${VERSION:(-6)}" != "-arm64" ]]; then
  VERSION="${VERSION}-arm64"
fi

# Remove images if they exist
for TAG in "${VERSION}" "latest"; do
  if docker rmi "${IMG}:${TAG}" >/dev/null 2>&1; then
    echo "Removed local image ${IMG}:${TAG}"
  else
    echo "Image ${IMG}:${TAG} was not found or failed to be removed."
  fi
done

# Verify that all images have been removed
for TAG in "${VERSION}" "latest"; do
  if [ -z "$(docker images -q "${IMG}:${TAG}")" ]; then
    echo "All ${IMG}:${TAG} local images have been deleted."
  else
    echo "Some ${IMG}:${TAG} local images are still present:"
    docker images "${IMG}:${TAG}"
    exit 1
  fi
done

# See https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md#full-tag-listing
docker buildx build --no-cache --load \
    --platform=linux/arm64 \
    --build-arg BUILD_IMAGE_TAG=9.0-noble-arm64v8 \
    --build-arg RUN_IMAGE_TAG=9.0-alpine-arm64v8 \
    -t "${IMG}:${VERSION}" \
    .

# Push images to Docker registry
echo "Pushing ${IMG}:${VERSION}..."
if ! docker push "${IMG}:${VERSION}"; then
  echo "Failed to push ${IMG}:${VERSION}."
  exit 1
fi

echo "Docker image push complete."
