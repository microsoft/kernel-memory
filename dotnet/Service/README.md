This folder contains **Semantic Memory Service**, used to manage memory
settings, ingest data and query for answers.

The service is composed by two main components:

* A web service to upload files, to ask questions, and to manage settings.
* A background asynchronous data pipeline to process the files uploaded.

If you need deploying and scaling the webservice and the pipeline handlers
separately, you can configure the service to enable/disable them.

# ⚙️ Configuration

To quickly set up the service, run the following command and follow the
questions on screen.

```bash
dotnet run setup
```

The app will create a configuration file `appsettings.Development.json`
that you can customize. Look at the comments in `appsettings.json` for
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

# ▶️ Start the service

To run the Semantic Memory service:

> ### On WSL / Linux / MacOS:
>
> ```shell
> cd dotnet/Service
> ./run.sh
> ```

> ### On Windows:
>
> ```shell
> cd dotnet/Service
> run.cmd
> ```

The `run.sh`/`run.cmd` scripts internally use the `ASPNETCORE_ENVIRONMENT` env var,
so the code will use the settings stored in `appsettings.Development.json`.

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

To use RabbitMQ locally, install docker and launch RabbitMQ with:

      docker run -it --rm --name rabbitmq \
         -p 5672:5672 -e RABBITMQ_DEFAULT_USER=user -e RABBITMQ_DEFAULT_PASS=password \
         rabbitmq:3
