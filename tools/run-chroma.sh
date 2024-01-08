#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE"

package="chromadb"

if ! python -c "import pkgutil; exit(0 if pkgutil.find_loader('$package') else 2)"; then
    echo "Installing $package..."
    if pip install $package; then
        echo "$package has been successfully installed."
    else
        echo "Failed to install $package."
    fi
fi

echo "# Chroma data path: ${HERE}chromadb"
chroma run --path ./chromadb
