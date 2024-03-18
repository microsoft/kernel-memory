---
nav_order: 10
parent: Quickstart
title: Bash and Curl examples
permalink: /quickstart/bash
layout: default
---
# Command line (bash, curl)

## Upload a document

Open a Bash console in the folder where you have cloned the Kernel Memory repository.

Inside the repository, you will find a **tools** folder containing a few scripts, such as `upload-file.sh` and `ask.sh`.
These scripts can be used to send requests to the web service. 
Alternatively, you can use `curl` to send requests. The syntax is straightforward, and the web service
responds with JSON.

Run the following commands:

    cd tools
    ./upload-file.sh -f README.md -i doc01 -s http://127.0.0.1:9001

or:

    curl -F 'file1=@"README.md"' -F 'documentId="doc01"' http://127.0.0.1:9001/upload

You should see a confirmation message:

{: .console }
> ```json
> {"index":"","documentId":"doc01","message":"Document upload completed, ingestion pipeline started"}
> ```

## Query

    cd tools
    ./ask.sh -q "Can I use KM from command line?" -s http://127.0.0.1:9001

or:

    curl -d'{"question":"Can I use KM from command line?"}' -H 'Content-Type: application/json' http://127.0.0.1:9001/ask

The script will show the JSON returned by the web service, and among other details you should see the answer:

{: .console }
> _Yes, you can use Kernel Memory (KM) from the command line. There are several scripts provided, such
as `upload-file.sh`,
> `ask.sh`, and `search.sh`, that allow you to interact with KM from the command line. These scripts provide
functionality
> for uploading files, asking questions about your documents, and searching your indexed documents, respectively.
> Additionally, there is a script called `run-qdrant.sh` that starts Qdrant, which is used to store and search vectors
in KM._