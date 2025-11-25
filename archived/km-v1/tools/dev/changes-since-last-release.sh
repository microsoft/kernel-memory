#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd ../.. && pwd)"
cd $ROOT

LAST_TAG="$(git tag -l|grep packages|sort -r|head -n 1)"
echo "# Last release: $LAST_TAG"

CMD='git log --pretty=oneline ${LAST_TAG}..HEAD -- .'

if [ -z "$(eval $CMD)" ]; then
  echo "# No changes since last release"
else
  echo "# Changes since last release:"
  eval $CMD
fi