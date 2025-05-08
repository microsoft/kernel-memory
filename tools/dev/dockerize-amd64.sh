#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/../.."

USR=kernelmemory
IMG=${USR}/service
ARCH=amd64

# Prompt user for VERSION
if [ -z "$1" ]; then
  # Get the latest git tag and remove any prefix
  LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
  SUGGESTED_VERSION=${LATEST_TAG#*-}
  read -p "Enter VERSION (default: '${SUGGESTED_VERSION}'): " VERSION
  VERSION=${VERSION:-$SUGGESTED_VERSION}
else
  VERSION=$1
  read -p "VERSION is set to '${VERSION}'. Press Enter to keep or provide a new value: " NEW_VERSION
  VERSION=${NEW_VERSION:-$VERSION}
fi

# Trim VERSION value
VERSION=$(echo "$VERSION" | xargs)

# Ensure VERSION ends with arch name
if [[ "${VERSION:(-6)}" != "-${ARCH}" ]]; then
  VERSION="${VERSION}-${ARCH}"
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
docker build --platform linux/amd64 \
    --build-arg BUILD_IMAGE_TAG=9.0-noble-amd64 \
    --build-arg RUN_IMAGE_TAG=9.0-alpine-amd64 \
    --push -t "${IMG}:${VERSION}" \
    .

# Push images to Docker registry
echo "Pushing ${IMG}:${VERSION}..."
if ! docker push "${IMG}:${VERSION}"; then
  echo "Failed to push ${IMG}:${VERSION}."
  exit 1
fi


echo "Docker image push complete."
