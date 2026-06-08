#!/bin/bash

# The name of the deployment to search for

# Run the command and save the output
OUTPUT=$(atlas deployments list)

echo "Output: "
echo $OUTPUT

# count line of output
LINE=$(echo "$OUTPUT" | wc -l)
echo "Count line of output: $LINE "

if [ $LINE -lt 2 ]; then
    echo "No deployment found. create a new one"
    atlas deployments setup local --mdbVersion 6.0 --bindIpAll --username root --password root --type local --force
else
    echo "Deployment found. Start it"
    atlas deployments start local
fi

function pause_atlas() {
    atlas deployments pause local
}
# This will call the 'on_exit' function when the container exits
trap pause_atlas EXIT

tail -f /dev/null