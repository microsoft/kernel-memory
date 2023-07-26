# Semantic Memory Service

**Semantic Memory** is an open-source library and service specializing in the
efficient indexing of datasets through custom continuous data pipelines.

Utilizing advanced embeddings and LLMs, the system enables natural language
querying for obtaining answers from the indexed data, complete with citations
and links to the original sources.

The solution is divided in two main areas: **Encoding** and **Retrieval**.

# Encoding

The encoding phase allows to ingest data and index it, using Embeddings and LLMs.

Documents are encoded using one or more "data pipelines" where consecutive
"handlers" take the input and process it, turning raw data into memories.

Pipelines can be customized, and they typically consist of:

* **storage**: store a copy of the document (if necessary, copies can be deleted
  after processing).
* text **extraction**: extract text from documents, presentations, etc.
* text **partitioning**: chunk the text in small blocks.
* text **indexing**: calculate embedding for each text block, store the embedding
  with a reference to the original document.

## Runtime mode

Encoding can run **in process**, e.g. running all the handlers synchronously,
in real time, as soon as some content is loaded/uploaded.
In this case the upload process and handlers must be written in the same
language, e.g. C#.

Encoding can also run **as a distributed service**, deployed locally or in
the cloud. This mode provides some important benefits:

* **Handlers can be written in different languages**, e.g. extract
  data using Python libraries, index using C#, etc. This can be useful when
  working with file types supported better by specific libraries available
  only in some programming language like Python.
* Content ingestion can be started using a **web service**. The repository
  contains a web service ready to use in C# and Python (work in progress).
  The web service can also be used by Copilot Chat, to store data and
  search for answers.
* Content processing runs **asynchronously** in the background, allowing
  to process several files in parallel, with support for retry logic.

# Retrieval

Memories can be retrieved using natural language queries. The service
also supports RAG, generating answers using prompts, relevant memories,
and plugins.

Similar to the encoding process, retrieval is available as a library and
as a web service.

# Folder structure

* `clients`: three different memory clients, to be used depending on your
  deployment:
  1. `curl`: command line tool to upload memories to the Semantic Memory web service.
  2. `MemoryWebClient`: .NET client to upload memories to the Semantic Memory web service.
  3. `MemoryPipelineClient`: .NET client to upload memory running all the import
     logic locally, without the need to run services.
  * `samples` folder: a few examples showing how to use the clients above.
* `server`: services you can deploy locally or in the cloud, to upload memories,
  process files asynchronously, answer questions. You can deploy the services in
  two ways:
  1. Monolithic service: deploy `CombinedServices` for .NET. A python version will
     be available soon.
  2. Separate service: deploy and scale `PipelineService` and `WebService` .NET
     projects individually. Python version will be available soon.
  * `samples` folder: a few examples about how to customize the memory ingestion
    pipeline with custom handlers.
  * `tools`: command line tools, e.g. scripts to start RabbitMQ locally as an
    alternative to Azure Queues.
* `lib`: code shared by clients and servers, .NET and Python.
