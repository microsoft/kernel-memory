# upload-file.sh

Simple client for command line uploads to Kernel Memory.

Instructions:

```bash
./upload-file.sh -h
```

# ask.sh

Simple client for asking questions about your documents from the command line.

Instructions:

```bash
./ask.sh -h
```

# search.sh

Simple client for searching your indexed documents from the command line.

Instructions:

```bash
./search.sh -h
```

# run-qdrant.sh

Script to start Qdrant using Docker, for local development/debugging.

Qdrant is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).

# run-rabbitmq.sh

Script to start RabbitMQ using Docker, for local development/debugging.

RabbitMQ is used to provides queues for the asynchronous pipelines,
as an alternative to
[Azure Queues](https://learn.microsoft.com/azure/storage/queues/storage-queues-introduction).

# run-redis.sh

Script to start Redis using Docker, for local development/debugging.
This will run Redis on port 6379, as well as running a popular Redis GUI, [RedisInsight](https://redis.com/redis-enterprise/redis-insight/), on port 8001.

Redis is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).