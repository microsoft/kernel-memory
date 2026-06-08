#!/usr/bin/env bash

# This script sends a query to Kernel Memory web service
# from the command line, using curl

set -e
trap 'exitScript' ERR

# Show manual
help() {
  cat <<-_EOF_
Help for Bash script

Usage:

    ./ask.sh -s <url> -q "<question>" [-p <index name>] [-f "<filter>"]

    -s web service URL     (required) Kernel Memory web service URL.
    -q question            (required) Question, using quotes.

    -p index               (optional) Index to search.
    -f filter              (optional) Key-value filter, e.g. -f '"type":["news","article"],"group":["emails"]'
                                      Note: multiple filters not yet supported by this client.

    -h                     Print this help content.


Example:

    ./ask.sh -s http://127.0.0.1:9001 -p mydata -q "tell me about Semantic Kernel"


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
            -q)
                shift
                QUESTION=$1
            ;;
            -p)
                shift
                INDEXNAME=$1
            ;;
            -f)
                shift
                FILTER=$1
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
    if [ -z "$QUESTION" ]; then
        echo "Please specify the user ID"
        exit 2
    fi
}

# Remove variables and functions from the environment, in case the script was sourced
cleanupEnv() {
    unset SERVICE_URL QUESTION INDEXNAME FILTER CMD
    unset -f help readParameters validateParameters cleanupEnv exitScript
}

# Clean exit for sourced scripts
exitScript() {
    cleanupEnv
    kill -SIGINT $$
}

readParameters "$@"
validateParameters

# Send HTTP request using curl
CMD="curl -v -H 'Content-Type: application/json'"
CMD="$CMD -d'{\"question\":\"${QUESTION}\",\"filters\":[{${FILTER}}],\"index\":\"${INDEXNAME}\",\"args\":{\"custom_rag_max_tokens_int\":1000}}'"
CMD="$CMD $SERVICE_URL/ask"

set -x
eval $CMD
