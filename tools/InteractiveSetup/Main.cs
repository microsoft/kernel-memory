// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.KernelMemory.InteractiveSetup.UI;
using Newtonsoft.Json.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup;

public static class Main
{
    private static BoundedBoolean s_cfgOpenAPI = new();

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
    private static BoundedBoolean s_cfgAzureOCR = new();

    // Vectors
    private static BoundedBoolean s_cfgAzureCognitiveSearch = new();
    private static BoundedBoolean s_cfgQdrant = new();
    private static BoundedBoolean s_cfgSimpleVectorDb = new();

    public static void InteractiveSetup(
        bool cfgService = false,
        bool cfgOrchestration = true)
    {
        s_cfgOpenAPI = new();

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
        s_cfgAzureOCR = new();

        // Vectors
        s_cfgAzureCognitiveSearch = new();
        s_cfgQdrant = new();
        s_cfgSimpleVectorDb = new();

        try
        {
            if (cfgService) { ServiceSetup(); }
            else { NoService(); }

            if (cfgOrchestration)
            {
                OrchestrationTypeSetup();
                QueuesSetup();
                AzureQueueSetup();
                RabbitMQSetup();
                SimpleQueuesSetup();
            }

            // Storage
            ContentStorageTypeSetup();
            AzureBlobsSetup();
            SimpleFileStorageSetup();

            // Image support
            OCRSetup();
            AzureCognitiveFormSetup();

            // Embedding generation
            EmbeddingGeneratorTypeSetup();
            AzureOpenAIEmbeddingSetup();
            OpenAISetup();

            // Embedding storage
            VectorDbTypeSetup();
            AzureCognitiveSearchSetup();
            QdrantSetup();
            SimpleVectorDbSetup();

            // Text generation
            TextGeneratorTypeSetup();
            AzureOpenAITextSetup();
            OpenAISetup();

            LoggerSetup();
        }
        catch (Exception e)
        {
            Console.WriteLine($"== Error: {e.GetType().FullName}");
            Console.WriteLine($"== {e.Message}");
        }

        SetupUI.Exit();
    }

    private static void ServiceSetup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Run the web service (upload and search endpoints)?",
            Options = new List<Answer>
            {
                new("Yes", () =>
                {
                    AppSettings.Change(x => { x.Service.RunWebService = true; });
                    s_cfgOpenAPI.Value = true;
                }),
                new("No", () =>
                {
                    AppSettings.Change(x => { x.Service.RunWebService = false; });
                    s_cfgOpenAPI.Value = false;
                }),
                new("-exit-", SetupUI.Exit),
            }
        });

        if (s_cfgOpenAPI.Value)
        {
            SetupUI.AskQuestionWithOptions(new QuestionWithOptions
            {
                Title = "Enable OpenAPI swagger doc at /swagger/index.html?",
                Options = new List<Answer>
                {
                    new("Yes", () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = true; }); }),
                    new("No", () => { AppSettings.Change(x => { x.Service.OpenApiEnabled = false; }); }),
                    new("-exit-", SetupUI.Exit),
                }
            });
        }

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Run the .NET pipeline handlers as a service?",
            Options = new List<Answer>
            {
                new("Yes", () => { AppSettings.Change(x => { x.Service.RunHandlers = true; }); }),
                new("No", () => { AppSettings.Change(x => { x.Service.RunHandlers = false; }); }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void NoService()
    {
        AppSettings.Change(x =>
        {
            x.Service.RunWebService = false;
            x.Service.RunHandlers = false;
            x.Service.OpenApiEnabled = false;
            x.DataIngestion.OrchestrationType = "InProcess";
            x.DataIngestion.DistributedOrchestration.QueueType = "";
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
                new("Trace", () => { logLevel = "Trace"; }),
                new("Debug", () => { logLevel = "Debug"; }),
                new("Information", () => { logLevel = "Information"; }),
                new("Warning", () => { logLevel = "Warning"; }),
                new("Error", () => { logLevel = "Error"; }),
                new("Critical", () => { logLevel = "Critical"; }),
                new("-exit-", SetupUI.Exit),
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
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which embedding generator should be used for the question?",
            Options = new List<Answer>
            {
                new("Azure OpenAI embedding model", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "AzureOpenAIEmbedding";
                        x.DataIngestion.EmbeddingGeneratorTypes = new List<string> { x.Retrieval.EmbeddingGeneratorType };
                    });
                    s_cfgAzureOpenAIEmbedding.Value = true;
                }),
                new("OpenAI embedding model", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.EmbeddingGeneratorType = "OpenAI";
                        x.DataIngestion.EmbeddingGeneratorTypes = new List<string> { x.Retrieval.EmbeddingGeneratorType };
                    });
                    s_cfgOpenAI.Value = true;
                }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void TextGeneratorTypeSetup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When generating synthetic data and answers, which LLM text generator should be used?",
            Options = new List<Answer>
            {
                new("Azure OpenAI text/chat model", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "AzureOpenAIText"; });
                    s_cfgAzureOpenAIText.Value = true;
                }),
                new("OpenAI text/chat model", () =>
                {
                    AppSettings.Change(x => { x.TextGeneratorType = "OpenAI"; });
                    s_cfgOpenAI.Value = true;
                }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void AzureOpenAIEmbeddingSetup()
    {
        if (!s_cfgAzureOpenAIEmbedding.Value) { return; }

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

    private static void AzureOpenAITextSetup()
    {
        if (!s_cfgAzureOpenAIText.Value) { return; }

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

    private static void OpenAISetup()
    {
        if (!s_cfgOpenAI.Value) { return; }

        s_cfgOpenAI.Value = false;
        const string ServiceName = "OpenAI";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "TextModel", "" },
                { "EmbeddingModel", "" },
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

    private static void OCRSetup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which service should be used to extract text from images?",
            Options = new List<Answer>
            {
                new("None", () =>
                {
                    AppSettings.Change(x => { x.ImageOcrType = "None"; });
                }),
                new("Azure Form Recognizer", () =>
                {
                    AppSettings.Change(x => { x.ImageOcrType = "AzureFormRecognizer"; });
                    s_cfgAzureOCR.Value = true;
                }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void AzureCognitiveFormSetup()
    {
        if (!s_cfgAzureOCR.Value) { return; }

        s_cfgAzureOCR.Value = false;
        const string ServiceName = "AzureFormRecognizer";

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
            { "Endpoint", SetupUI.AskOpenQuestion("Azure Cognitive Services <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure Cognitive Services <API Key>", config["APIKey"].ToString()) },
        });
    }

    private static void OrchestrationTypeSetup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "How should memory ingestion be orchestrated?",
            Options = new List<Answer>
            {
                new("Using asynchronous distributed queues (allows to mix handlers written in different languages)",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.OrchestrationType = "Distributed"; });
                        s_cfgQueue.Value = true;
                    }),
                new("In process orchestration, all .NET handlers run synchronously",
                    () => { AppSettings.Change(x => { x.DataIngestion.OrchestrationType = "InProcess"; }); }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void QueuesSetup()
    {
        if (!s_cfgQueue.Value) { return; }

        s_cfgQueue.Value = false;
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Which queue service will be used?",
            Options = new List<Answer>
            {
                new("Azure Queue",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "AzureQueue"; });
                        s_cfgAzureQueue.Value = true;
                    }),
                new("RabbitMQ",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "RabbitMQ"; });
                        s_cfgRabbitMq.Value = true;
                    }),
                new("SimpleQueues (local file system, only for tests)",
                    () =>
                    {
                        AppSettings.Change(x => { x.DataIngestion.DistributedOrchestration.QueueType = "SimpleQueues"; });
                        s_cfgSimpleQueues.Value = true;
                    }),
                new("-exit-", SetupUI.Exit),
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
            config = new Dictionary<string, object> { { "Directory", "" } };
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
        const string ServiceName = "AzureQueue";

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

    private static void RabbitMQSetup()
    {
        if (!s_cfgRabbitMq.Value) { return; }

        s_cfgRabbitMq.Value = false;
        const string ServiceName = "RabbitMq";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object>
            {
                { "Host", "127.0.0.1" },
                { "Port", "5672" },
                { "Username", "user" },
                { "Password", "" },
            };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Host", SetupUI.AskOpenQuestion("RabbitMQ <host>", config["Host"].ToString()) },
            { "Port", SetupUI.AskOpenQuestion("RabbitMQ <TCP port>", config["Port"].ToString()) },
            { "Username", SetupUI.AskOpenQuestion("RabbitMQ <username>", config["Username"].ToString()) },
            { "Password", SetupUI.AskPassword("RabbitMQ <password>", config["Password"].ToString()) },
        });
    }

    private static void ContentStorageTypeSetup()
    {
        if (!s_cfgContentStorage.Value) { return; }

        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "Where should the service store files?",
            Options = new List<Answer>
            {
                new("Azure Blobs", () =>
                {
                    AppSettings.Change(x => { x.ContentStorageType = "AzureBlobs"; });
                    s_cfgAzureBlobs.Value = true;
                }),
                new("SimpleFileStorage (local file system)", () =>
                {
                    AppSettings.Change(x => { x.ContentStorageType = "SimpleFileStorage"; });
                    s_cfgSimpleFileStorage.Value = true;
                }),
                new("-exit-", SetupUI.Exit),
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
            config = new Dictionary<string, object> { { "Directory", "" } };
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

    private static void VectorDbTypeSetup()
    {
        SetupUI.AskQuestionWithOptions(new QuestionWithOptions
        {
            Title = "When searching for answers, which vector DB service contains embeddings to search?",
            Options = new List<Answer>
            {
                new("Azure Cognitive Search", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.VectorDbType = "AzureCognitiveSearch";
                        x.DataIngestion.VectorDbTypes = new List<string> { x.Retrieval.VectorDbType };
                    });
                    s_cfgAzureCognitiveSearch.Value = true;
                }),
                new("Qdrant", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.VectorDbType = "Qdrant";
                        x.DataIngestion.VectorDbTypes = new List<string> { x.Retrieval.VectorDbType };
                    });
                    s_cfgQdrant.Value = true;
                }),
                new("SimpleVectorDb (file based vector DB, only for tests)", () =>
                {
                    AppSettings.Change(x =>
                    {
                        x.Retrieval.VectorDbType = "SimpleVectorDb";
                    });
                    s_cfgSimpleVectorDb.Value = true;
                }),
                new("-exit-", SetupUI.Exit),
            }
        });
    }

    private static void SimpleVectorDbSetup()
    {
        if (!s_cfgSimpleVectorDb.Value) { return; }

        s_cfgSimpleVectorDb.Value = false;
        const string ServiceName = "SimpleVectorDb";

        if (!AppSettings.GetCurrentConfig().Services.TryGetValue(ServiceName, out var config))
        {
            config = new Dictionary<string, object> { { "Directory", "" } };
        }

        AppSettings.Change(x => x.Services[ServiceName] = new Dictionary<string, object>
        {
            { "Directory", SetupUI.AskOpenQuestion("Directory where to store vectors", config["Directory"].ToString()) }
        });
    }

    private static void AzureCognitiveSearchSetup()
    {
        if (!s_cfgAzureCognitiveSearch.Value) { return; }

        s_cfgAzureCognitiveSearch.Value = false;
        const string ServiceName = "AzureCognitiveSearch";

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
            { "Endpoint", SetupUI.AskOpenQuestion("Azure Cognitive Search <endpoint>", config["Endpoint"].ToString()) },
            { "APIKey", SetupUI.AskPassword("Azure Cognitive Search <API Key>", config["APIKey"].ToString()) },
        });
    }

    private static void QdrantSetup()
    {
        if (!s_cfgQdrant.Value) { return; }

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
}
