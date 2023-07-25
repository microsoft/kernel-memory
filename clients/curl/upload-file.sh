#!/usr/bin/env bash

# This script is an example showing how to upload a file to
# Semantic Memory web service from the command line, using curl

set -e
trap 'exitScript' ERR

# Show manual
help() {
  cat <<-_EOF_
Help for Bash script

Usage:

    ./upload-file.sh -f <file path> -u <id> -c <list> -i <id> -s <url>

    -f file path           Path to the document to upload.
    -u userId              User ID.
    -c "coll1 coll2 .."    List of collection IDs separated by a space.
    -i uploadId            Unique identifier for the upload.
    -s web service URL     Semantic Memory web service URL.
    -h                     Print this help content.


Example:

    ./upload-file.sh -f myFile.pdf -u me -c "notes meetings" -i "bash test" -s http://127.0.0.1:9001/upload


For more information visit https://github.com/microsoft/semantic-memory
_EOF_
}

# Read command line parameters
readParameters() {
  while [ "$1" != "" ]; do
    case $1 in
    -f)
      shift
      FILENAME=$1
      ;;
    -u)
      shift
      USER_ID=$1
      ;;
    -c)
      shift
      COLLECTIONS=$1
      ;;
    -i)
      shift
      REQUEST_ID=$1
      ;;
    -s)
      shift
      SERVICE_URL=$1
      ;;
    *)
      help
      exitScript
      ;;
    esac
    shift
  done
}

validatePrameters() {
  if [ -z "$FILENAME" ]; then
    echo "Please specify a file to upload"
    exit 1
  fi
  if [ -d "$FILENAME" ]; then
    echo "$FILENAME is a directory."
    exit 1
  fi
  if [ ! -f "$FILENAME" ]; then
    echo "$FILENAME does not exist."
    exit 1
  fi
  if [ -z "$FILENAME" ]; then
    echo "Please specify a file to upload"
    help
    exit 1
  fi
  if [ -z "$USER_ID" ]; then
    echo "Please specify the user ID"
    exit 2
  fi
  if [ -z "$COLLECTIONS" ]; then
    echo "Please specify the list of collection IDs"
    exit 3
  fi
  if [ -z "$REQUEST_ID" ]; then
    echo "Please specify a unique upload request ID"
    exit 4
  fi
  if [ -z "$SERVICE_URL" ]; then
    echo "Please specify the web service URL"
    exit 5
  fi
}

# Remove variables and functions from the environment, in case the script was sourced
cleanupEnv() {
  unset FILENAME USER_ID COLLECTIONS REQUEST_ID SERVICE_URL
  unset -f help readParameters validatePrameters cleanupEnv exitScript
}

# Clean exit for sourced scripts
exitScript() {
  cleanupEnv
  kill -SIGINT $$
}

readParameters "$@"
validatePrameters

# Handle list of vault IDs
COLLECTIONS_FIELD=""
for x in $COLLECTIONS; do
  COLLECTIONS_FIELD="${COLLECTIONS_FIELD} -F vaults=\"${x}\""
done

# Send HTTP request using curl
#set -x
curl -v \
  -F 'file1=@"'"${FILENAME}"'"' \
  -F 'user="'"${USER_ID}"'"' \
  -F 'requestId="'"${REQUEST_ID}"'"' \
  $COLLECTIONS_FIELD \
  $SERVICE_URL
