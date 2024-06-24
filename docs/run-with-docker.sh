#!/usr/bin/env bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && cd .. && pwd)"
cd $ROOT
# ===============================================================

cd docs

mkdir -p ".bundles_cache"

docker run -it --rm -v "$PWD:/srv/jekyll" \
    -p 4000:4000 \
    -e BUNDLE_PATH="/srv/jekyll/.bundles_cache" \
    jekyll/builder:4 \
    bash -c "gem install bundler && bundle install && bundle exec Jekyll serve --host 0.0.0.0 --verbose --config _config.yml"
