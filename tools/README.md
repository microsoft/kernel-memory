# Kernel memory web service scripts

### upload-file.sh

Simple client for command line uploads to Kernel Memory.

Instructions:

```bash
./upload-file.sh -h
```

### ask.sh

Simple client for asking questions about your documents from the command line.

Instructions:

```bash
./ask.sh -h
```

### search.sh

Simple client for searching your indexed documents from the command line.

Instructions:

```bash
./search.sh -h
```

# Vector DB scripts

### run-elasticsearch.sh

Script to start Elasticsearch using Docker for local development/debugging.

Elasticsearch is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).

### run-mssql.sh

Script to start MS SQL using Docker for local development/debugging.

MS SQL is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).

### run-qdrant.sh

Script to start Qdrant using Docker, for local development/debugging.

Qdrant is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).

### run-redis.sh

Script to start Redis using Docker, for local development/debugging.
This will run Redis on port 6379, as well as running a popular Redis
GUI, [RedisInsight](https://redis.com/redis-enterprise/redis-insight/), on port 8001.

Redis is used to store and search vectors, as an alternative to
[Azure AI Search](https://azure.microsoft.com/products/ai-services/ai-search/).

# Orchestration queues scripts

### run-rabbitmq.sh

Script to start RabbitMQ using Docker, for local development/debugging.

RabbitMQ is used to provides queues for the asynchronous pipelines,
as an alternative to
[Azure Queues](https://learn.microsoft.com/azure/storage/queues/storage-queues-introduction).

# Kernel memory runtime scripts

### run-km-service.sh

Script to start KM service from source code, using KM nuget packages where configured as such.

### run-km-service-from-source.sh

Script to start KM service from local source code, ignoring KM nuget packages.

### setup-km-service.sh

Script to start KM service configuration wizard and create an appsettings.Development.json file.
