# upload-file.sh

Simple client for command line uploads to Semantic Memory.

Instructions:

```bash
./upload-file.sh -h
```

Example:

```bash
./upload-file.sh -f test.pdf -s http://127.0.0.1:9001/upload -u curlUser -c curlDataCollection -i curlExample01
```

# run-rabbitmq.sh

Script to start RabbitMQ using Docker, for local development/debugging.

RabbitMQ is used to provides queues for the asynchronous pipelines, as an alternative
to Azure Queues.