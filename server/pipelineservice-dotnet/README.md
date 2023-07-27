This folder contains **Semantic Pipeline Service**, responsible for asynchronously
processing data, extracting text and embeddings, and populating the vector DB using
default and/or custom handlers.

If you prefer deploying and scaling the webservice and the pipeline handlers
together, see the [CombinedServices](../combinedservices-dotnet/) project.

# ⚙️ Configuration

To quickly set up the service, run the following command and follow the
questions on screen.

```bash
dotnet run setup
```

The app will create a configuration file `appsettings.Development.json`
that you can customize. Look at the comments in `appsettings.json` for more
details and more advanced options.

Configuration settings can be saved in four places:

1. `appsettings.json`: although possible, it's not recommended, to avoid
   risks of leaking secrets in source code repositories.
2. `appsettings.Development.json`: this works only when the environment
   variable `ASPNETCORE_ENVIRONMENT` is set to `Development`.
3. `appsettings.Production.json`: this works only when the environment
   variable `ASPNETCORE_ENVIRONMENT` is set to `Production`.
4. using **env vars**: preferred method for credentials. Any setting in
   appsettings.json can be overridden by env vars. The env var name correspond
   to the configuration key name, using `__` (double underscore) as a separator.

# ⚙️ Dependencies

The service depends on three main components:

* **Content storage**: this is where content like files, chats, emails are saved
  and transformed when uploaded. Currently, the solution supports local
  filesystem and Azure Blobs.
* **Vector storage**: service used to persist embeddings. Currently, the solution
  support Azure Cognitive Search. Soon we'll add support for Qdrant, Pinecone,
  Chroma and more.
* **Data ingestion orchestration**: this can run in memory and in the same
  process, e.g. when working with small files, or run as a service, in which
  case it requires persistent queues like Azure Queues or RabbitMQ. 

**The pipeline service is designed to run in the background and in the cloud,
without direct interaction. We recommended using it with asynchronous queues
and cloud storage, for better resiliency and data consistency.**

To use RabbitMQ locally, install docker and launch RabbitMQ with:

      docker run -it --rm --name rabbitmq \
         -p 5672:5672 -e RABBITMQ_DEFAULT_USER=user -e RABBITMQ_DEFAULT_PASS=password \
         rabbitmq:3
