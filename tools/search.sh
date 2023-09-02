#!/usr/bin/env bash

# This script sends a query to Semantic Memory web service
# from the command line, using curl

set -e
trap 'exitScript' ERR

# Show manual
help() {
  cat <<-_EOF_
Help for Bash script

Usage:

    ./search.sh -s <url> -q "<query>" [-p <index name>] [-f "<filter>"] [-l <number of results>]

    -s web service URL     (required) Semantic Memory web service URL.
    -q query               (required) Text to search, using quotes.

    -p index               (optional) Index to search.
    -f filter              (optional) Key-value filter, e.g. -f '"type":["news","article"],"group":["emails"]'
    -l limit               (optional) Max number of results, e.g. -f 1 to get only the top result

    -h                     Print this help content.


Example:

    ./search.sh -s http://127.0.0.1:9001 -p mydata -l 1 -q "Semantic Kernel"


For more information visit https://github.com/microsoft/semantic-memory
_EOF_
}

# Read command line parameters
readParameters() {
  MAX_RESULTS="-1"
  while [ "$1" != "" ]; do
    case $1 in
    -s)
      shift
      SERVICE_URL=$1
      ;;
    -q)
      shift
      QUERY=$1
      ;;
    -p)
      shift
      INDEXNAME=$1
      ;;
    -f)
      shift
      FILTER=$1
      ;;
    -l)
      shift
      MAX_RESULTS="$1"
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
  if [ -z "$QUERY" ]; then
    echo "Please specify the user ID"
    exit 2
  fi
}

# Remove variables and functions from the environment, in case the script was sourced
cleanupEnv() {
  unset SERVICE_URL QUERY INDEXNAME FILTER MAX_RESULTS
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
set -x
curl -v -H 'Content-Type: application/json' \
    -d'{"query":"'"${QUERY}"'","index":"'"${INDEXNAME}"'","filter":{'"${FILTER}"'},"limit":'${MAX_RESULTS}'}' \
    $SERVICE_URL/search
