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

    ./ask.sh -s <url> -u <id> -q "<question>" -f "<filter>"

    -s web service URL     (required) Semantic Memory web service URL.
    -u userId              (required) User ID.
    -q question            (required) Question, using quotes.
    -f filter              (optional) Key-value filter, e.g. -f '"type":["news","article"],"group":["emails"]'

    -h                     Print this help content.


Example:

    ./ask.sh -s http://127.0.0.1:9001 -u me -q "tell me about Semantic Kernel"


For more information visit https://github.com/microsoft/semantic-memory
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
    -u)
      shift
      USER_ID=$1
      ;;
    -q)
      shift
      QUESTION=$1
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
  if [ -z "$USER_ID" ]; then
    echo "Please specify the user ID"
    exit 2
  fi
  if [ -z "$QUESTION" ]; then
    echo "Please specify the user ID"
    exit 2
  fi
}

# Remove variables and functions from the environment, in case the script was sourced
cleanupEnv() {
  unset SERVICE_URL USER_ID QUESTION FILTER
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
    -d'{"question":"'"${QUESTION}"'","userId":"'"${USER_ID}"'","filter":{'"${FILTER}"'}}' \
    $SERVICE_URL/ask
