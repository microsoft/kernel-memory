# Kernel Memory as a Service

This folder contains **Kernel Memory Service**, used to manage memory
settings, ingest data and query for answers.

The service is composed by two main components:

1. A web service to upload files, to ask questions.
2. A background asynchronous data pipeline to process the files uploaded.

If you need deploying and scaling the webservice and the pipeline handlers
separately, you can enable/disable each of them via configuration.

Once the service is up and running, you can use the **Kernel Memory web
client** or simply interact with the Web API. The API schema is available
at http://127.0.0.1:9001/swagger/index.html when running the service locally
with **OpenAPI** enabled.

# ▶️ Docker support

If you're looking for a Docker image, we publish a build [here](https://hub.docker.com/r/kernelmemory/service) and
you can use the [Dockerfile](https://github.com/microsoft/kernel-memory/blob/main/Dockerfile) in the repo for custom builds.

You can test the image in demo mode passing the OPENAI_API_KEY environment variable:

```
docker run -e OPENAI_API_KEY="..." -p 9001:9001 -it --rm kernelmemory/service
```

otherwise for a full setup, after creating a configuration file:

on Windows:

    docker run --volume .\appsettings.Development.json:/app/appsettings.Production.json -it --rm -p 9001:9001 kernelmemory/service

on macOS/Linux:

    docker run --volume ./appsettings.Development.json:/app/appsettings.Production.json -it --rm -p 9001:9001 kernelmemory/service

# ⚙️ Configuration

To quickly set up the service, run the following command and follow the
questions on screen.

```bash
dotnet run setup
```

The wizard will create a configuration file `appsettings.Development.json`
that you can customize. Look at the comments in `appsettings.json` for
details and more advanced options.

Configuration settings can be saved in multiple places, each source can also override the previous
(ie environment variables can be used to override settings in the config files):

1. `appsettings.json`: although possible, it's not recommended, to avoid risks of leaking secrets
   in source code repositories. The file is mandatory and is used only for default settings.
2. `appsettings.Development.json`: this is used only when the environment variable `ASPNETCORE_ENVIRONMENT` is set to `Development`.
3. `appsettings.Production.json`: this is used only when the environment variable `ASPNETCORE_ENVIRONMENT` is set to `Production`.
4. [.NET Secret Manager](https://learn.microsoft.com/aspnet/core/security/app-secrets#secret-manager)
5. using **env vars**: preferred method for credentials. Any setting in  appsettings.json can be overridden by env vars.
   The env var name corresponds to the configuration key name, using `__` (double underscore) as a separator instead of `:`.
   For instance `Logging:LogLevel:Default` is set with `Logging__LogLevel__Default`.

# ▶️ Start the service from source

To run the Kernel Memory service:

> ### On WSL / Linux / MacOS:
>
> ```shell
> cd service/Service
> ./run.sh
> ```

> ### On Windows:
>
> ```shell
> cd service\Service
> run.cmd
> ```

The `run.sh`/`run.cmd` scripts internally use the `ASPNETCORE_ENVIRONMENT`
env var, so the code will use the settings stored in `appsettings.Development.json`.

# ⚙️ Dependencies

The service depends on three main components:

* **Content storage**: this is where content like files, chats, emails are
  saved and transformed when uploaded. Currently, the solution supports Azure Blobs,
  local filesystem and in-memory volatile filesystem.


* **Embedding generator**: all the documents uploaded are automatically
  partitioned (aka "chunked") and indexed for vector search, generating
  several embedding vectors for each file. We recommend using
  [OpenAI ADA v2](https://platform.openai.com/docs/guides/embeddings/what-are-embeddings)
  model, though you can easily plug in any embedding generator if needed.


* **Text generator** (aka LLM): during document ingestion and when asking
  questions, the service requires an LLM to execute prompts, e.g. to
  generate synthetic data, and to generate answers. The service has
  been tested with OpenAI
  [GPT3.5 and GPT4](https://platform.openai.com/docs/models/overview)
  which we recommend. The number of tokens available is also an important
  factor affecting summarization and answer generations, so you might
  get better results with 16k, 32k and bigger models.


* **Vector storage**: service used to persist embeddings. The
  service supports **Azure AI Search**, **Qdrant**, **Redis** and other engines,
  plus  a very basic in memory vector storage with support for persistence on disk
  called **SimpleVectorDb**. Unless configured differently, KM uses SimpleVectorDb
  storing data in memory only.


* **Data ingestion orchestration**: this can run in memory and in the same
  process, e.g. when working with small files, or run as a service, in which
  case it requires persistent queues like **Azure Queues** or **RabbitMQ**.
  The Core assembly/package includes also a basic in memory queue called
  **SimpleQueues** that might be useful for tests and demos.

  When running the service, we recommend persistent queues for reliability and
  horizontal scaling, like Azure Queues and RabbitMQ.
