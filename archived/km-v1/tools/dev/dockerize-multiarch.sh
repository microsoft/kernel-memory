#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE/../.."

USR=kernelmemory
IMG=${USR}/service

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
echo "# Pulling images..."
docker pull --platform linux/amd64 "kernelmemory/service:${VERSION}-amd64"
docker pull --platform linux/arm64 "kernelmemory/service:${VERSION}-arm64"

# Delete manifest
echo "# Deleting manifest..."
docker manifest rm kernelmemory/service:latest || true

# Create manifest
echo "# Creating new manifest..."
docker manifest create kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-amd64" \
    "kernelmemory/service:${VERSION}-arm64"
  
# Add images to manifest
echo "# Adding images to new manifest..."
docker manifest annotate kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-amd64" --os linux --arch amd64
docker manifest annotate kernelmemory/service:latest \
    "kernelmemory/service:${VERSION}-arm64" --os linux --arch arm64

# Publish manifest
echo "# Publishing manifest..."
docker manifest push kernelmemory/service:latest


echo "Docker image push complete."
