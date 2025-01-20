#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/../.."

USR=kernelmemory
IMG=${USR}/service

# Prompt user for VERSION
read -p "Enter VERSION (e.g. '0.99.260214.1'): " VERSION

# Remove local images if they exist
for TAG in "latest" "${VERSION}-amd64" "${VERSION}-arm64"; do
  if docker rmi "${IMG}:${TAG}" >/dev/null 2>&1; then
    echo "Removed local image ${IMG}:${TAG}"
  else
    echo "Image ${IMG}:${TAG} was not found or failed to be removed."
  fi
done

# Check that all images have been removed
for TAG in "latest" "${VERSION}-amd64" "${VERSION}-arm64"; do
  if [ -z "$(docker images -q "${IMG}:${TAG}")" ]; then
    echo "All ${IMG}:${TAG} local images have been deleted."
  else
    echo "Some ${IMG}:${TAG} local images are still present:"
    docker images "${IMG}:${TAG}"
    exit 1
  fi
done

# Pull images
docker pull --platform linux/amd64 "kernelmemory/service:${VERSION}-amd64"
docker pull --platform linux/arm64 "kernelmemory/service:${VERSION}-arm64"

# Create manifest
docker manifest create kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-amd64" \
    "kernelmemory/service:${VERSION}-arm64"
  
# Add images to manifest
docker manifest annotate kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-amd64" --os linux --arch amd64
docker manifest annotate kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-arm64" --os linux --arch arm64

# Publish manifest
docker manifest push kernelmemory/service:latest


echo "Docker image push complete."
