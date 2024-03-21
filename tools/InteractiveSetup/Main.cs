// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup;

public static class Main
{
    private static BoundedBoolean s_cfgWebService = new();

    // Storage
    private static BoundedBoolean s_cfgContentStorage = new();
    private static BoundedBoolean s_cfgAzureBlobs = new();
    private static BoundedBoolean s_cfgSimpleFileStorage = new();

    // Queues
    private static BoundedBoolean s_cfgQueue = new();
    private static BoundedBoolean s_cfgAzureQueue = new();
    private static BoundedBoolean s_cfgRabbitMq = new();
    private static BoundedBoolean s_cfgSimpleQueues = new();

    // AI
    private static BoundedBoolean s_cfgAzureOpenAIText = new();
    private static BoundedBoolean s_cfgAzureOpenAIEmbedding = new();
    private static BoundedBoolean s_cfgOpenAI = new();
    private static BoundedBoolean s_cfgLlamaSharp = new();
    private static BoundedBoolean s_cfgAzureAIDocIntel = new();

    // Vectors
    private static BoundedBoolean s_cfgEmbeddingGenerationEnabled = new();
    private static BoundedBoolean s_cfgAzureAISearch = new();
    private static BoundedBoolean s_cfgQdrant = new();
    private static BoundedBoolean s_cfgPostgres = new();
    private static BoundedBoolean s_cfgRedis = new();
    private static BoundedBoolean s_cfgSimpleVectorDb = new();

    public static void InteractiveSetup(string[] args)
    {
        // If args is not empty, then the user is asking to configure a specific list of services
        if (args.Length > 0)
        {
            ConfigureItem(args);
            SetupUI.Exit();
        }

        s_cfgWebService = new();

        // Storage
        s_cfgContentStorage = new(initialState: true);
        s_cfgAzureBlobs = new();
        s_cfgSimpleFileStorage = new();

        // Queues
        s_cfgQueue = new();
        s_cfgAzureQueue = new();
        s_cfgRabbitMq = new();
        s_cfgSimpleQueues = new();

        // AI
        s_cfgAzureOpenAIText = new();
        s_cfgAzureOpenAIEmbedding = new();
        s_cfgOpenAI = new();
        s_cfgLlamaSharp = new();
        s_cfgAzureAIDocIntel = new();

        // Vectors
        s_cfgEmbeddingGenerationEnabled = new(initialState: true);
        s_cfgAzureAISearch = new();
        s_cfgPostgres = new();
        s_cfgQdrant = new();
        s_cfgRedis = new();
        s_cfgSimpleVectorDb = new();

        try
        {
            ServiceSetup();
            WebserviceSetup();

            // Orchestration
            QueuesSetup();
            AzureQueueSetup();
            RabbitMQSetup();
            SimpleQueuesSetup();

            // Storage
            ContentStorageTypeSetup();
            AzureBlobsSetup();
            SimpleFileStorageSetup();

            // Image support
            OCRSetup();
            AzureAIDocIntelSetup();

            // Embedding generation
            EmbeddingGeneratorTypeSetup();
            AzureOpenAIEmbeddingSetup();
            OpenAISetup();

            // Memory DB
            MemoryDbTypeSetup();
            AzureAISearchSetup();
            QdrantSetup();
            PostgresSetup();
            RedisSetup();
            SimpleVectorDbSetup();

            // Text generation
            TextGeneratorTypeSetup();
            AzureOpenAITextSetup();
            OpenAISetup();
            LlamaSharpSetup();

            LoggerSetup();

            Console.WriteLine("== Done! :-)\n");
            Console.WriteLine("== You can start the service with: dotnet run\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"== Error: {e.GetType().FullName}");
            Console.WriteLine($"== {e.Message}");
        }

        SetupUI.Exit();
    }

    private static void ConfigureItem(string[] items)
    {
        foreach (var itemName in items)
        {
            switch (itemName)
            {
                case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                    AzureAISearchSetup(true);
                    break;

                case string x when x.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                    AzureOpenAIEmbeddingSetup(true);
                    break;

                case string x when x.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                    AzureOpenAITextSetup(true);
                    break;

                case string x when x.Equals("LlamaSharp", StringComparison.OrdinalIgnoreCase):
                    LlamaSharpSetup(true);
                    break;

                case string x when x.Equals("MemoryDbType", StringComparison.OrdinalIgnoreCase):
                    MemoryDbTypeSetup();
                    break;

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                    OpenAISetup(true);
                    break;

                case string x when x.Equals("Postgres", StringComparison.OrdinalIgnoreCase):
                    PostgresSetup(true);
                    break;

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                    QdrantSetup(true);
                    break;

                case string x when x.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    RabbitMQSetup(true);
                    break;

                case string x when x.Equals("Redis", StringComparison.OrdinalIgnoreCase):
                    RedisSetup(true);
                    break;

                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                    SimpleVectorDbSetup(true);
                    break;
            }
        }
    }

    private static void ServiceSetup()
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "How should Kernel Memory service run and handle memory and documents ingestion?",
            Description = "KM provides a HTTP web service for uploading documents, searching, asking questions, etc. The " +
                          "service can be configured to run ingestion (loading documents) asynchronously or synchronously. " +
                          "When running asynchronously, handlers run in the background and use distributed queues to enable " +
                          "long running tasks, to retry in case of errors, and to allow scaling the service horizontally. " +
                          "The web service can also be disabled in case the queued jobs are populated differently.",
            Options = new List<Answer>
            {
                new("Web Service with Asynchronous Ingestion Handlers (better for retry logic and long operations)",
                    config.Service.RunWebService && config.Service.RunHandlers,
                    () =>
                    {
                        s_cfgWebService.Value = true;
                        s_cfgQueue.Value = true;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = true;
                            x.Service.RunHandlers = true;
                            x.DataIngestion.OrchestrationType = "Distributed";
                        });
                    }),
                new("Web Service with Synchronous Ingestion Handlers",
                    config.Service.RunWebService && !config.Service.RunHandlers,
                    () =>
                    {
                        s_cfgWebService.Value = true;
                        s_cfgQueue.Value = false;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = true;
                            x.Service.RunHandlers = false;
                            x.DataIngestion.OrchestrationType = "InProcess";
                            x.DataIngestion.DistributedOrchestration.QueueType = "";
                        });
                    }),
                new("No web Service, run only asynchronous Ingestion Handlers in the background",
                    !config.Service.RunWebService && config.Service.RunHandlers,
                    () =>
                    {
                        s_cfgWebService.Value = false;
                        s_cfgQueue.Value = true;
                        AppSettings.Change(x =>
                        {
                            x.Service.RunWebService = false;
                            x.Service.RunHandlers = true;
                            x.DataIngestion.OrchestrationType = "Distributed";
                        });
                    }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void WebserviceSetup(bool force = false)
    {
        if (!s_cfgWebService.Value && !force) { return; }

        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Protect the web service with API Keys?",
            Description = "If the web service runs on a public network it should protected requiring clients to pass one of two secret API keys on each request. The API Key is passed using the `Authorization` HTTP header.",
            Options = new List<Answer>
            {
                new("Yes", config.ServiceAuthorization.Enabled, () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.ServiceAuthorization.Enabled = true;
                        x.ServiceAuthorization.HttpHeaderName = "Authorization";
                        x.ServiceAuthorization.AccessKey1 = SetupUI.AskPassword("API Key 1 (min 32 chars, alphanumeric ('- . _' allowed))", x.ServiceAuthorization.AccessKey1);
                        x.ServiceAuthorization.AccessKey2 = SetupUI.AskPassword("API Key 2 (min 32 chars, alphanumeric ('- . _' allowed))", x.ServiceAuthorization.AccessKey2);
                    });
                }),
                new("No", !config.ServiceAuthorization.Enabled, () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.ServiceAuthorization.Enabled = false;
                        x.ServiceAuthorization.HttpHeaderName = "Authorization";
                        x.ServiceAuthorization.AccessKey1 = "";
                        x.ServiceAuthorization.AccessKey2 = "";
                    });
                }),
                new("-exit-", false, SetupUI.Exit),
            }
        });

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Enable OpenAPI swagger doc at /swagger/index.html?",
            Options = new List<Answer>
            {
                new("Yes", config.Service.OpenApiEnabled, () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = true; }); }),
                new("No", !config.Service.OpenApiEnabled, () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = false; }); }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void LoggerSetup()
    {
        string logLevel = "Debug";
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Log level?",
            Options = new List<Answer>
            {
                new("Trace", false, () => { logLevel = "Trace"; }),
                new("Debug", false, () => { logLevel = "Debug"; }),
                new("Information", false, () => { logLevel = "Information"; }),
                new("Warning", true, () => { logLevel = "Warning"; }),
                new("Error", false, () => { logLevel = "Error"; }),
                new("Critical", false, () => { logLevel = "Critical"; }),
                new("-exit-", false, SetupUI.Exit),
            }
        });

        AppSettings.GlobalChange(data =>
        {
            if (data["Logging"] == null) { data["Logging"] = new JObject(); }

            if (data["Logging"]!["LogLevel"] == null)
            {
                data["Logging"]!["LogLevel"] = new JObject { ["Microsoft.AspNetCore"] = "Warning" };
            }

            data["Logging"]!["LogLevel"]!["Default"] = logLevel;
        });
    }

    private static void EmbeddingGeneratorTypeSetup()
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When importing data, generate embeddings, or let the memory Db class take care of it?",
            Options = new List<Answer>
            {
                new("Yes, generate embeddings", config.DataIngestion.EmbeddingGenerationEnabled, () =>
                {
                    AppSettings.Change(x => x.DataIngestion.EmbeddingGenerationEnabled = true);
                    s_cfgEmbeddingGenerationEnabled.Value = true;
                }),
                new("No, my memory Db class/engine takes care of it", !config.DataIngestion.EmbeddingGenerationEnabled, () =>
                {
                    AppSettings.Change(x => x.DataIngestion.EmbeddingGenerationEnabled = false);
                    s_cfgEmbeddingGenerationEnabled.Value = false;
                })
            }
        });

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for text and/or answers, which embedding generator should be used for vector search?",
            Options = new List<Answer>
            {
                new("Azure OpenAI embedding model", config.Retrieval.EmbeddingGeneratorType == "AzureOpenAIEmbedding", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "AzureOpenAIEmbedding";
                        x.DataIngestion.EmbeddingGeneratorTypes = s_cfgEmbeddingGenerationEnabled.Value
                            ? new List<string> { x.Retrieval.EmbeddingGeneratorType }
                            : new List<string> { };
                    });
                    s_cfgAzureOpenAIEmbedding.Value = true;
                }),
                new("OpenAI embedding model", config.Retrieval.EmbeddingGeneratorType == "OpenAI", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "OpenAI";
                        x.DataIngestion.EmbeddingGeneratorTypes = s_cfgEmbeddingGenerationEnabled.Value
                            ? new List<string> { x.Retrieval.EmbeddingGeneratorType }
                            : new List<string> { };
                    });
                    s_cfgOpenAI.Value = true;
                }),
                new("None/Custom (manually set with code)", string.IsNullOrEmpty(config.Retrieval.EmbeddingGeneratorType), () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "";
                        x.DataIngestion.EmbeddingGeneratorTypes = new List<string> { };
                    });
                }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void TextGeneratorTypeSetup()
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When generating answers and synthetic data, which LLM text generator should be used?",
            Options = new List<Answer>
            {
                new("Azure OpenAI text/chat model", config.TextGeneratorType == "AzureOpenAIText", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "AzureOpenAIText"; });
                    s_cfgAzureOpenAIText.Value = true;
                }),
                new("OpenAI text/chat model", config.TextGeneratorType == "OpenAI", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "OpenAI"; });
                    s_cfgOpenAI.Value = true;
                }),
                new("LLama model", config.TextGeneratorType == "LlamaSharp", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "LlamaSharp"; });
                    s_cfgLlamaSharp.Value = true;
                }),
                new("None/Custom (manually set with code)", string.IsNullOrEmpty(config.TextGeneratorType), () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = ""; });
                }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void AzureOpenAIEmbeddingSetup(bool force = false)
    {
        if (!s_cfgAzureOpenAIEmbedding.Value && !force) { return; }

        s_cfgAzureOpenAIEmbedding.Value = false;
        const string ServiceName = "AzureOpenAIEmbedding";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "Deployment", "" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "APIType", "EmbeddingGeneration" },
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", config["Endpoint"].ToString()) },
            { "Deployment", SetupUI.AskOpenQuestion("Azure OpenAI <embedding model deployment name>", config["Deployment"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure OpenAI <API Key>", config["APIKey"].ToString()) },
        });
    }

    private static void AzureOpenAITextSetup(bool force = false)
    {
        if (!s_cfgAzureOpenAIText.Value && !force) { return; }

        s_cfgAzureOpenAIText.Value = false;
        const string ServiceName = "AzureOpenAIText";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "APIType", "ChatCompletion" },
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "Deployment", "" },
                { "APIKey", "" },
                { "MaxRetries", 10 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "APIType", "ChatCompletion" },
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure OpenAI <endpoint>", config["Endpoint"].ToString()) },
            { "Deployment", SetupUI.AskOpenQuestion("Azure OpenAI <text/chat completion deployment name>", config["Deployment"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure OpenAI <API Key>", config["APIKey"].ToString()) },
            { "MaxRetries", 10 },
        });
    }

    private static void OpenAISetup(bool force = false)
    {
        if (!s_cfgOpenAI.Value && !force) { return; }

        s_cfgOpenAI.Value = false;
        const string ServiceName = "OpenAI";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "TextModel", "gpt-3.5-turbo-16k" },
                { "EmbeddingModel", "text-embedding-ada-002" },
                { "APIKey", "" },
                { "OrgId", "" },
                { "MaxRetries", 10 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "TextModel", SetupUI.AskOpenQuestion("OpenAI <text/chat model name>", config.TryGet("TextModel")) },
            { "EmbeddingModel", SetupUI.AskOpenQuestion("OpenAI <embedding model name>", config.TryGet("EmbeddingModel")) },
            { "APIKey", SetupUI.AskPassword("OpenAI <API Key>", config.TryGet("APIKey")) },
            { "OrgId", SetupUI.AskOptionalOpenQuestion("Optional OpenAI <Organization Id>", config.TryGet("OrgId")) },
            { "MaxRetries", 10 },
        });
    }

    private static void LlamaSharpSetup(bool force = false)
    {
        if (!s_cfgLlamaSharp.Value && !force) { return; }

        s_cfgLlamaSharp.Value = false;
        const string ServiceName = "LlamaSharp";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "ModelPath", "" },
                { "MaxTokenTotal", 4096 },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "ModelPath", SetupUI.AskOpenQuestion("Path to model .gguf file", config.TryGet("ModelPath")) },
            { "MaxTokenTotal", SetupUI.AskOpenQuestion("Max tokens supported by the model", config.TryGet("MaxTokenTotal")) },
        });
    }

    private static void OCRSetup()
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which service should be used to extract text from images?",
            Options = new List<Answer>
            {
                new("None", config.DataIngestion.ImageOcrType == "None", () =>
                {
                    AppSettings.Change(x => { x.DataIngestion.ImageOcrType = "None"; });
                }),
                new("Azure AI Document Intelligence", config.DataIngestion.ImageOcrType == "AzureAIDocIntel", () =>
                {
                    AppSettings.Change(x => { x.DataIngestion.ImageOcrType = "AzureAIDocIntel"; });
                    s_cfgAzureAIDocIntel.Value = true;
                }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void AzureAIDocIntelSetup()
    {
        if (!s_cfgAzureAIDocIntel.Value) { return; }

        s_cfgAzureAIDocIntel.Value = false;
        const string ServiceName = "AzureAIDocIntel";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure AI <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure AI <API Key>", config["APIKey"].ToString()) },
        });
    }

    private static void QueuesSetup()
    {
        if (!s_cfgQueue.Value) { return; }

        var config = AppSettings.GetCurrentConfig();

        s_cfgQueue.Value = false;
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which queue service will be used?",
            Options = new List<Answer>
            {
                new("Azure Queue",
                    config.DataIngestion.DistributedOrchestration.QueueType == "AzureQueues",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "AzureQueues"; });
                        s_cfgAzureQueue.Value = true;
                    }),
                new("RabbitMQ",
                    config.DataIngestion.DistributedOrchestration.QueueType == "RabbitMQ",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "RabbitMQ"; });
                        s_cfgRabbitMq.Value = true;
                    }),
                new("SimpleQueues (only for tests, data stored in memory or disk, see config file)",
                    config.DataIngestion.DistributedOrchestration.QueueType == "SimpleQueues",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "SimpleQueues"; });
                        s_cfgSimpleQueues.Value = true;
                    }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void SimpleQueuesSetup()
    {
        if (!s_cfgSimpleQueues.Value) { return; }

        s_cfgSimpleQueues.Value = false;
        const string ServiceName = "SimpleQueues";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" },
                { "StorageType", "Volatile" }
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store queue messages", config["Directory"].ToString()) }
        });
    }

    private static void AzureQueueSetup()
    {
        if (!s_cfgAzureQueue.Value) { return; }

        s_cfgAzureQueue.Value = false;
        const string ServiceName = "AzureQueues";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ConnectionString" },
                { "Account", "" },
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ConnectionString" },
            { "Account", SetupUI.AskOpenQuestion("Azure Queue <account name>", config["Account"].ToString()) },
            { "ConnectionString", SetupUI.AskPassword("Azure Queue <connection string>", config["ConnectionString"].ToString()) },
        });
    }

    private static void RabbitMQSetup(bool force = false)
    {
        if (!s_cfgRabbitMq.Value && !force) { return; }

        s_cfgRabbitMq.Value = false;
        const string ServiceName = "RabbitMQ";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Host", "127.0.0.1" },
                { "Port", "5672" },
                { "Username", "user" },
                { "Password", "" },
                { "VirtualHost", "/" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Host", SetupUI.AskOpenQuestion("RabbitMQ <host>", config["Host"].ToString()) },
            { "Port", SetupUI.AskOpenQuestion("RabbitMQ <TCP port>", config["Port"].ToString()) },
            { "Username", SetupUI.AskOpenQuestion("RabbitMQ <username>", config["Username"].ToString()) },
            { "Password", SetupUI.AskPassword("RabbitMQ <password>", config["Password"].ToString()) },
            { "VirtualHost", SetupUI.AskOpenQuestion("RabbitMQ <virtualhost>", config["VirtualHost"].ToString()) },
        });
    }

    private static void ContentStorageTypeSetup()
    {
        if (!s_cfgContentStorage.Value) { return; }

        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Where should the service store files?",
            Options = new List<Answer>
            {
                new("Azure Blobs",
                    config.ContentStorageType == "AzureBlobs",
                    () =>
                    {
                        AppSettings.Change(x => { x.ContentStorageType = "AzureBlobs"; });
                        s_cfgAzureBlobs.Value = true;
                    }),
                new("SimpleFileStorage (only for tests, data stored in memory or disk, see config file)",
                    config.ContentStorageType == "SimpleFileStorage",
                    () =>
                    {
                        AppSettings.Change(x => { x.ContentStorageType = "SimpleFileStorage"; });
                        s_cfgSimpleFileStorage.Value = true;
                    }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void SimpleFileStorageSetup()
    {
        if (!s_cfgSimpleFileStorage.Value) { return; }

        s_cfgSimpleFileStorage.Value = false;
        const string ServiceName = "SimpleFileStorage";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" },
                { "StorageType", "Volatile" }
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store files", config["Directory"].ToString()) }
        });
    }

    private static void AzureBlobsSetup()
    {
        if (!s_cfgAzureBlobs.Value) { return; }

        s_cfgAzureBlobs.Value = false;
        const string ServiceName = "AzureBlobs";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ConnectionString" },
                { "Account", "" },
                { "Container", "smemory" },
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ConnectionString" },
            { "Container", SetupUI.AskOpenQuestion("Azure Blobs <container name>", config["Container"].ToString()) },
            { "Account", SetupUI.AskOpenQuestion("Azure Blobs <account name>", config["Account"].ToString()) },
            { "ConnectionString", SetupUI.AskPassword("Azure Blobs <connection string>", config["ConnectionString"].ToString()) },
        });
    }

    private static void MemoryDbTypeSetup()
    {
        var config = AppSettings.GetCurrentConfig();

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which memory DB service contains the records to search?",
            Options = new List<Answer>
            {
                new("Azure AI Search",
                    config.Retrieval.MemoryDbType == "AzureAISearch",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "AzureAISearch";
                            x.DataIngestion.MemoryDbTypes = new List<string> { x.Retrieval.MemoryDbType };
                        });
                        s_cfgAzureAISearch.Value = true;
                    }),
                new("Qdrant",
                    config.Retrieval.MemoryDbType == "Qdrant",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Qdrant";
                            x.DataIngestion.MemoryDbTypes = new List<string> { x.Retrieval.MemoryDbType };
                        });
                        s_cfgQdrant.Value = true;
                    }),
                new("Postgres",
                    config.Retrieval.MemoryDbType == "Postgres",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Postgres";
                            x.DataIngestion.MemoryDbTypes = new List<string> { x.Retrieval.MemoryDbType };
                        });
                        s_cfgPostgres.Value = true;
                    }),
                new("Redis",
                    config.Retrieval.MemoryDbType == "Redis",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "Redis";
                            x.DataIngestion.MemoryDbTypes = new List<string> { x.Retrieval.MemoryDbType };
                        });
                        s_cfgRedis.Value = true;
                    }),
                new("SimpleVectorDb (only for tests, data stored in memory or disk, see config file)",
                    config.Retrieval.MemoryDbType == "SimpleVectorDb",
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "SimpleVectorDb";
                            x.DataIngestion.MemoryDbTypes = new List<string> { x.Retrieval.MemoryDbType };
                        });
                        s_cfgSimpleVectorDb.Value = true;
                    }),
                new("None/Custom (manually set in code)",
                    string.IsNullOrEmpty(config.Retrieval.MemoryDbType),
                    () =>
                    {
                        AppSettings.Change(x =>
                        {
                            x.Retrieval.MemoryDbType = "";
                            x.DataIngestion.MemoryDbTypes = new List<string> { };
                        });
                    }),
                new("-exit-", false, SetupUI.Exit),
            }
        });
    }

    private static void SimpleVectorDbSetup(bool force = false)
    {
        if (!s_cfgSimpleVectorDb.Value && !force) { return; }

        s_cfgSimpleVectorDb.Value = false;
        const string ServiceName = "SimpleVectorDb";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Directory", "" },
                { "StorageType", "Volatile" }
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store vectors", config["Directory"].ToString()) }
        });
    }

    private static void AzureAISearchSetup(bool force = false)
    {
        if (!s_cfgAzureAISearch.Value && !force) { return; }

        s_cfgAzureAISearch.Value = false;
        const string ServiceName = "AzureAISearch";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Auth", "ApiKey" },
                { "Endpoint", "" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Auth", "ApiKey" },
            { "Endpoint", SetupUI.AskOpenQuestion("Azure AI Search <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure AI Search <API Key>", config["APIKey"].ToString()) },
        });
    }

    private static void PostgresSetup(bool force = false)
    {
        if (!s_cfgPostgres.Value && !force) { return; }

        s_cfgPostgres.Value = false;
        const string ServiceName = "Postgres";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "ConnectionString", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "ConnectionString", SetupUI.AskPassword("Postgres connection string (e.g. 'Host=..;Port=5432;Username=..;Password=..')", config["ConnectionString"].ToString(), optional: true) },
        });
    }

    private static void QdrantSetup(bool force = false)
    {
        if (!s_cfgQdrant.Value && !force) { return; }

        s_cfgQdrant.Value = false;
        const string ServiceName = "Qdrant";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Endpoint", "http://127.0.0.1:6333" },
                { "APIKey", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Endpoint", SetupUI.AskOpenQuestion("Qdrant <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Qdrant <API Key> (for cloud only)", config["APIKey"].ToString(), optional: true) },
        });
    }

    private static void RedisSetup(bool force = false)
    {
        if (!s_cfgRedis.Value && !force) { return; }

        s_cfgRedis.Value = false;
        const string ServiceName = "Redis";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                ["ConnectionString"] = ""
            };
        }

        var connectionString = SetupUI.AskPassword("Redis connection string (e.g. 'localhost:6379,password=..')", config["ConnectionString"].ToString(), optional: true);

        bool AskMoreTags(string additionalMessage)
        {
            string answer = "No";
            SetupUI.AskQuestionWithOptions(new QuestionWithOptions
            {
                Title = $"{additionalMessage}[Redis] Do you want to add a tag (or another tag) to filter memory records?",
                Options = new List<Answer>
                {
                    new("Yes", false, () => { answer = "Yes"; }),
                    new("No", true, () => { answer = "No"; }),
                }
            });

            return answer.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        }

        Dictionary<string, string> tagFields = new();

        string additionalMessage = string.Empty;
        while (AskMoreTags(additionalMessage))
        {
            var tagName = SetupUI.AskOpenQuestion("Enter the name of the tag you'd like to filter on, e.g. username", string.Empty);
            if (string.IsNullOrEmpty(tagName))
            {
                additionalMessage = "Unusable tag name entered. ";
                continue;
            }

            var separatorChar = SetupUI.AskOptionalOpenQuestion("How do you want to separate tag values (default is ',')?", ",");
            if (separatorChar.Length > 1)
            {
                additionalMessage = "Unusable separator Char entered. ";
                continue;
            }

            tagFields.Add(tagName, separatorChar);
            additionalMessage = string.Empty;
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Tags", tagFields },
            { "ConnectionString", connectionString },
        });
    }
}
