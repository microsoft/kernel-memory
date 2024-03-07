#!/usr/bin/env bash

# This script uploads a file to Kernel Memory web service
# from the command line, using curl

set -e
trap 'exitScript' ERR

# Show manual
help() {
  cat <<-_EOF_
Help for Bash script

Usage:

    ./upload-file.sh -s <url> -f <file path> [-p <index name>] [-i <id>] [-t <tag1> -t <tag2> -t <tag3> (...)]

    -s web service URL     (required) Kernel Memory web service URL.
    -f file path           (required) Path to the document to upload.

    -p index               (optional) Index where to store memories.
    -i document ID         (optional) Unique identifier for the document uploaded.
    -t "key:value"         (optional) Key-Value tag. Multiple tags and values per tag can be set.

    -h                     Print this help content.


Example:

    ./upload-file.sh -s http://127.0.0.1:9001 -f myFile.pdf -u me -t "type:notes" -t "type:test" -i "bash test"


For more information visit https://github.com/microsoft/kernel-memory
_EOF_
}

# Read command line parameters
readParameters() {
    while [ "$1" != "" ]; do
        case $1 in
            -s)
                shift
                SERVICE_URL=$1
            ;;
            -f)
                shift
                FILENAME=$1
            ;;
            -p)
                shift
                INDEXNAME=$1
            ;;
            -i)
                shift
                DOCUMENT_ID=$1
            ;;
            -t)
                shift
                TAGS="$TAGS $1"
            ;;
            *)
                help
                exitScript
            ;;
        esac
        shift
    done
}

validateParameters() {
    if [ -z "$SERVICE_URL" ]; then
        echo "Please specify the web service URL"
        exit 1
    fi
    if [ -z "$FILENAME" ]; then
        echo "Please specify a file to upload"
        exit 3
    fi
    if [ -d "$FILENAME" ]; then
        echo "$FILENAME is a directory."
        exit 3
    fi
    if [ ! -f "$FILENAME" ]; then
        echo "$FILENAME does not exist."
        exit 3
    fi
}

# Remove variables and functions from the environment, in case the script was sourced
cleanupEnv() {
    unset SERVICE_URL FILENAME INDEXNAME DOCUMENT_ID TAGS TAG CMD
    unset -f help readParameters validateParameters cleanupEnv exitScript
}

# Clean exit for sourced scripts
exitScript() {
    cleanupEnv
    kill -SIGINT $$
}

readParameters "$@"
validateParameters

# Prepare curl command
CMD="curl -v -F 'file1=@\"${FILENAME}\"' -F 'index=\"${INDEXNAME}\"' -F 'documentId=\"${DOCUMENT_ID}\"' -F 'tags=\"${TAGS}\"'"

# Add URL
CMD="$CMD $SERVICE_URL/upload"

# Send HTTP request using curl
set -x
eval $CMD
