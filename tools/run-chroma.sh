#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
cd "$HERE"

PACKAGE="chromadb"
DATA=".chromadb"
CMD="python3"

if ! [ -x "$(command -v python3)" ]; then
  if ! [ -x "$(command -v python)" ]; then
    echo ""
    echo "ERROR: Python is not installed." >&2
    echo ""
    exit 1
  fi
  CMD="python"
fi

if ! $CMD -c "import pkgutil; exit(0 if pkgutil.find_loader('$PACKAGE') else 2)"; then
  echo ""
  echo "ERROR: $PACKAGE python package not available."
  echo ""
  echo "We recommend creating a python environment and installing $PACKAGE there."
  echo "If the package is already installed, activate the environment and retry."
  echo ""
  echo "Example:"
  echo ""
  echo "# == Installation"
  echo "#    $CMD -m venv .chromaenv"
  echo "#    source .chromaenv/bin/activate"
  echo "#    pip install --upgrade pip chromadb"
  echo ""
  echo "# == Start"
  echo "#    source .chromaenv/bin/activate"
  echo "#    source ${BASH_SOURCE[0]}"
  
  exit 1
fi

echo ""
echo "# Chroma data path: ${HERE}${DATA}"
echo "# If the command fails with 'command not found', try activating the python environment where Chroma was installed, then run the script again"
echo ""
chroma run --path ./${DATA}
