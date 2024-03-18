---
nav_order: 1
parent: Quickstart
title: Configuration
permalink: /quickstart/configuration
layout: default
---
# Configuration

KM service requires a configuration file to start, typically named `appsettings.Development.json` for local development
or `appsettings.Production.json` for production environments. The appropriate configuration file is automatically
loaded, based on the `ASPNETCORE_ENVIRONMENT` environment variable. If your development workstation doesn't have this
environment variable, it's recommended to create it: ASPNETCORE_ENVIRONMENT == "Development". In a production
environment you'll want to set ASPNETCORE_ENVIRONMENT = "Production".

The KM repository includes a setup wizard to help you create your initial `appsettings.Development.json` file:

    cd service/Service
    dotnet run setup

Follow the on-screen prompts to create or edit the configuration file. If needed, you can interrupt the script and
run it multiple times to modify the settings.

{: .console }
> _Run the web service (upload and search endpoints)_? **YES**
>
> _Protect the web service with API Keys_? **NO**
> ------ you can leave this off for this test
>
> _Enable OpenAPI swagger doc at /swagger/index.html_? **YES**
>
> _Run the .NET pipeline handlers as a service_? **YES**
>
> _How should memory ingestion be orchestrated_? **Using asynchronous distributed queues**
>
> _Which queue service will be used_? **SimpleQueues**
> ------ this will use volatile queues in memory, suitable only for tests
>
>       Directory where to store queue messages: _tmp_queues
>
> _Where should the service store files_? **SimpleFileStorage**
> ------ this will use volatile storage in memory, suitable only for tests. You can manually edit appsettings.development.json to persist files on disk.
>
>       Directory where to store files: _tmp_files
>
> _Which service should be used to extract text from images_? **None**
>
> _When importing data, generate embeddings_? **YES**
>
> _When searching for text and/or answers, which embedding generator should be used_? **Azure OpenAI or OpenAI**
>
>       OpenAI <text/chat model name> [current: gpt-3.5-turbo-16k]:      Press ENTER to use default
>       OpenAI <embedding model name> [current: text-embedding-ada-002]: Press ENTER to use default
>       OpenAI <API Key>: sk-*********************************
>
> _When searching for answers, which memory DB service contains the records?_ **SimpleVectorDb**
> ------ this will use volatile storage in memory, suitable only for tests. You can manually edit
appsettings.development.json to persist files on disk, or choose one of the available options suggested.
>
>       Directory where to store vectors: _tmp_vectors
>
> _When generating answers and synthetic data, which LLM text generator should be used?_ **Azure OpenAI or OpenAI**
>
> _Log level_? **Information**

Great! If you completed the wizard, you should be ready to start the service and run the examples below. 

{: .important }
> * If you selected any of the "simpleXYZ" dependencies, then data will be stored in memory only, and automatically discarded
>   when the service stops. Edit the configuration file manually to persist  data on disk. [More information here](service/configuration).
> * The configuration wizard uses some default settings that you might want to change. After running the examples, take
>   a look at the included [appsettings.json](https://github.com/microsoft/kernel-memory/blob/main/service/Service/appsettings.json)
>   to see all the available options, and read the [Service Configuration doc](service/configuration) for more information.


## Next: [Start the service](start-service)
