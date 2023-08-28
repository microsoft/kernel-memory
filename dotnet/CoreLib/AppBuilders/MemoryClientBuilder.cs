// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.AI;
using Microsoft.SemanticMemory.Configuration;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.ContentStorage.DevTools;
using Microsoft.SemanticMemory.DataFormats.Image;
using Microsoft.SemanticMemory.DataFormats.Image.AzureFormRecognizer;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.MemoryStorage.DevTools;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant;
using Microsoft.SemanticMemory.Pipeline;
using Microsoft.SemanticMemory.Pipeline.Queue;
using Microsoft.SemanticMemory.Pipeline.Queue.AzureQueues;
using Microsoft.SemanticMemory.Pipeline.Queue.DevTools;
using Microsoft.SemanticMemory.Pipeline.Queue.RabbitMq;
using Microsoft.SemanticMemory.Search;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

public class MemoryClientBuilder
{
    private readonly IServiceCollection? _sharedServiceCollection;
    private const string ConfigRoot = "SemanticMemory";

    private enum ClientTypes
    {
        Undefined,
        SyncServerless,
        AsyncService,
    }

    private readonly WebApplicationBuilder _appBuilder;
    private WebApplication? _app = null;
    private readonly List<ITextEmbeddingGeneration> _embeddingGenerators = new();
    private readonly List<ISemanticMemoryVectorDb> _vectorDbs = new();

    public IServiceCollection Services
    {
        get => this._appBuilder.Services;
    }

    public MemoryClientBuilder(IServiceCollection? sharedServiceCollection = null)
    {
        this._sharedServiceCollection = sharedServiceCollection;
        this._appBuilder = WebApplication.CreateBuilder();

        // List of embedding generators and vector DBs used during the ingestion
        this._embeddingGenerators.Clear();
        this._vectorDbs.Clear();
        this.AddSingleton<List<ITextEmbeddingGeneration>>(this._embeddingGenerators);
        this.AddSingleton<List<ISemanticMemoryVectorDb>>(this._vectorDbs);

        // Default configuration for tests and demos
        this.WithDefaultMimeTypeDetection();
        this.WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = "tmp-memory-files" });
        this.WithSimpleVectorDb(new SimpleVectorDbConfig { Directory = "tmp-memory-vectors" });
    }

    public MemoryClientBuilder(WebApplicationBuilder appBuilder)
    {
        this._appBuilder = appBuilder;

        // List of embedding generators and vector DBs used during the ingestion
        this._embeddingGenerators.Clear();
        this._vectorDbs.Clear();
        this.AddSingleton<List<ITextEmbeddingGeneration>>(this._embeddingGenerators);
        this.AddSingleton<List<ISemanticMemoryVectorDb>>(this._vectorDbs);

        // Default configuration for tests and demos
        this.WithDefaultMimeTypeDetection();
        this.WithSimpleFileStorage(new SimpleFileStorageConfig { Directory = Path.Join(Path.GetTempPath(), "content") });
    }

    public MemoryClientBuilder WithCustomIngestionQueueClientFactory(QueueClientFactory service)
    {
        service = service ?? throw new ConfigurationException("The ingestion queue client factory instance is NULL");
        this.AddSingleton<QueueClientFactory>(service);
        return this;
    }

    public MemoryClientBuilder WithCustomStorage(IContentStorage service)
    {
        service = service ?? throw new ConfigurationException("The content storage instance is NULL");
        this.AddSingleton<IContentStorage>(service);
        return this;
    }

    public MemoryClientBuilder WithDefaultMimeTypeDetection()
    {
        this.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();

        return this;
    }

    public MemoryClientBuilder WithCustomMimeTypeDetection(IMimeTypeDetection service)
    {
        service = service ?? throw new ConfigurationException("The MIME type detection instance is NULL");
        this.AddSingleton<IMimeTypeDetection>(service);
        return this;
    }

    public MemoryClientBuilder WithCustomEmbeddingGeneration(ITextEmbeddingGeneration service, bool useForIngestion = true, bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("The embedding generator instance is NULL");

        if (useForRetrieval)
        {
            this.AddSingleton<ITextEmbeddingGeneration>(service);
        }

        if (useForIngestion)
        {
            this._embeddingGenerators.Add(service);
        }

        return this;
    }

    public MemoryClientBuilder WithCustomVectorDb(ISemanticMemoryVectorDb service, bool useForIngestion = true, bool useForRetrieval = true)
    {
        service = service ?? throw new ConfigurationException("The vector DB instance is NULL");

        if (useForRetrieval)
        {
            this.AddSingleton<ISemanticMemoryVectorDb>(service);
        }

        if (useForIngestion)
        {
            this._vectorDbs.Add(service);
        }

        return this;
    }

    public MemoryClientBuilder WithCustomTextGeneration(ITextGeneration service)
    {
        service = service ?? throw new ConfigurationException("The text generator instance is NULL");
        this.AddSingleton<ITextGeneration>(service);
        return this;
    }

    public MemoryClientBuilder WithCustomImageOcr(IOcrEngine service)
    {
        service = service ?? throw new ConfigurationException("The OCR engine instance is NULL");
        this.AddSingleton<IOcrEngine>(service);
        return this;
    }

    public MemoryClientBuilder FromAppSettings()
    {
        var config = this._appBuilder.Configuration.GetSection(ConfigRoot).Get<SemanticMemoryConfig>();
        if (config == null) { throw new ConfigurationException("Unable to parse configuration files"); }

        this.WithDefaultMimeTypeDetection();

        // Ingestion queue
        if (string.Equals(config.DataIngestion.OrchestrationType, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            switch (config.DataIngestion.DistributedOrchestration.QueueType)
            {
                case string y when y.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                    this._appBuilder.Services.AddAzureQueue(this.GetServiceConfig<AzureQueueConfig>(config, "AzureQueue"));
                    this._sharedServiceCollection?.AddAzureQueue(this.GetServiceConfig<AzureQueueConfig>(config, "AzureQueue"));
                    break;

                case string y when y.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    this._appBuilder.Services.AddRabbitMq(this.GetServiceConfig<RabbitMqConfig>(config, "RabbitMq"));
                    this._sharedServiceCollection?.AddRabbitMq(this.GetServiceConfig<RabbitMqConfig>(config, "RabbitMq"));
                    break;

                case string y when y.Equals("SimpleQueues", StringComparison.OrdinalIgnoreCase):
                    this._appBuilder.Services.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>(config, "SimpleQueues"));
                    this._sharedServiceCollection?.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>(config, "SimpleQueues"));
                    break;

                default:
                    // NOOP - allow custom implementations, via WithCustomIngestionQueueClientFactory()
                    break;
            }
        }

        // Storage
        switch (config.ContentStorageType)
        {
            case string x when x.Equals("AzureBlobs", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddAzureBlobAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>(config, "AzureBlobs"));
                this._sharedServiceCollection?.AddAzureBlobAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>(config, "AzureBlobs"));
                break;

            case string x when x.Equals("SimpleFileStorage", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>(config, "SimpleFileStorage"));
                this._sharedServiceCollection?.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>(config, "SimpleFileStorage"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomStorage()
                break;
        }

        // Retrieval embeddings
        switch (config.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                this._sharedServiceCollection?.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                this._sharedServiceCollection?.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                break;
        }

        // Ingestion embeddings
        foreach (var type in config.DataIngestion.EmbeddingGeneratorTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
                case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                {
                    var tmpBuilder = WebApplication.CreateBuilder();
                    tmpBuilder.Services.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                    var tmpApp = tmpBuilder.Build();
                    var service = tmpApp.Services.GetService<ITextEmbeddingGeneration>() ?? throw new ConfigurationException("Unable to build embedding generator");
                    this._embeddingGenerators.Add(service);

                    break;
                }

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                {
                    var tmpBuilder = WebApplication.CreateBuilder();
                    tmpBuilder.Services.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                    var tmpApp = tmpBuilder.Build();
                    var service = tmpApp.Services.GetService<ITextEmbeddingGeneration>() ?? throw new ConfigurationException("Unable to build embedding generator");
                    this._embeddingGenerators.Add(service);
                    break;
                }

                default:
                    // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                    break;
            }
        }

        // Retrieval Vector DB
        switch (config.Retrieval.VectorDbType)
        {
            case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                this._sharedServiceCollection?.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                break;

            case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                this._sharedServiceCollection?.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomVectorDb()
                break;
        }

        // Ingestion Vector DB
        foreach (var type in config.DataIngestion.VectorDbTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureCognitiveSearch", StringComparison.OrdinalIgnoreCase):
                {
                    var tmpBuilder = WebApplication.CreateBuilder();
                    tmpBuilder.Services.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                    var tmpApp = tmpBuilder.Build();
                    var service = tmpApp.Services.GetService<ISemanticMemoryVectorDb>() ?? throw new ConfigurationException("Unable to build ingestion vector DB");
                    this._vectorDbs.Add(service);
                    break;
                }

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                {
                    var tmpBuilder = WebApplication.CreateBuilder();
                    tmpBuilder.Services.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                    var tmpApp = tmpBuilder.Build();
                    var service = tmpApp.Services.GetService<ISemanticMemoryVectorDb>() ?? throw new ConfigurationException("Unable to build ingestion vector DB");
                    this._vectorDbs.Add(service);
                    break;
                }

                default:
                    // NOOP - allow custom implementations, via WithCustomVectorDb()
                    break;
            }
        }

        // Text generation
        switch (config.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIText"));
                this._sharedServiceCollection?.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIText"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                this._sharedServiceCollection?.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomTextGeneration()
                break;
        }

        // Image OCR
        switch (config.ImageOcrType)
        {
            case string y when string.IsNullOrWhiteSpace(y):
            case string x when x.Equals("None", StringComparison.OrdinalIgnoreCase):
                break;

            case string x when x.Equals("AzureFormRecognizer", StringComparison.OrdinalIgnoreCase):
                this._appBuilder.Services.AddAzureFormRecognizer(this.GetServiceConfig<AzureFormRecognizerConfig>(config, "AzureFormRecognizer"));
                this._sharedServiceCollection?.AddAzureFormRecognizer(this.GetServiceConfig<AzureFormRecognizerConfig>(config, "AzureFormRecognizer"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomImageOCR()
                break;
        }

        return this;
    }

    public MemoryClientBuilder Complete()
    {
        switch (this.GetBuildType())
        {
            case ClientTypes.SyncServerless:
                return this.CompleteServerlessClient();

            case ClientTypes.AsyncService:
                return this.CompleteAsyncClient();
        }

        return this;
    }

    public ISemanticMemoryClient Build()
    {
        switch (this.GetBuildType())
        {
            case ClientTypes.SyncServerless:
                return this.BuildServerlessClient();

            case ClientTypes.AsyncService:
                return this.BuildAsyncClient();

            case ClientTypes.Undefined:
                throw new SemanticMemoryException("Missing dependencies or insufficient configuration provided. " +
                                                  "Try using With...() methods " +
                                                  $"and other configuration methods before calling {nameof(this.Build)}(...)");

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IPipelineOrchestrator GetOrchestrator()
    {
        if (this._app == null)
        {
            throw new ConfigurationException("Memory instance not ready, call Build() first.");
        }

        return this._app.Services.GetService<IPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
    }

    public static ISemanticMemoryClient BuildWebClient(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ConfigurationException("The endpoint provided is empty");
        }

        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException("The endpoint is missing the protocol, specify either https:// or http://");
        }

        if (endpoint.Equals("http://", StringComparison.OrdinalIgnoreCase) || endpoint.Equals("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException("The endpoint is incomplete");
        }

        return new MemoryWebClient(endpoint);
    }

    public Memory BuildServerlessClient()
    {
        try
        {
            this.CompleteServerlessClient();
            this._app = this._appBuilder.Build();

            // In case the user didn't set the embedding generator and vector DB to use for ingestion, use the values set for retrieval 
            this.ReuseRetrievalEmbeddingGeneratorIfNecessary(this._app.Services);
            this.ReuseRetrievalVectorDbIfNecessary(this._app.Services);

            var orchestrator = this._app.Services.GetService<InProcessPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
            var searchClient = this._app.Services.GetService<SearchClient>() ?? throw new ConfigurationException("Unable to build search client");
            var ocrEngine = this._app.Services.GetService<IOcrEngine>();

            return new Memory(orchestrator, searchClient, ocrEngine);
        }
        catch (Exception e)
        {
            ShowException(e);
            throw;
        }
    }

    public MemoryService BuildAsyncClient()
    {
        this.CompleteAsyncClient();
        var app = this._appBuilder.Build();

        // In case the user didn't set the embedding generator and vector DB to use for ingestion, use the values set for retrieval 
        this.ReuseRetrievalEmbeddingGeneratorIfNecessary(app.Services);
        this.ReuseRetrievalVectorDbIfNecessary(app.Services);

        var orchestrator = app.Services.GetService<DistributedPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
        var searchClient = app.Services.GetService<SearchClient>() ?? throw new ConfigurationException("Unable to build search client");

        return new MemoryService(orchestrator, searchClient);
    }

    private MemoryClientBuilder CompleteServerlessClient()
    {
        this.RequireOneEmbeddingGenerator();
        this.RequireOneVectorDb();
        this.AddSingleton<SearchClient, SearchClient>();
        this.AddSingleton<IPipelineOrchestrator, InProcessPipelineOrchestrator>();
        this.AddSingleton<InProcessPipelineOrchestrator, InProcessPipelineOrchestrator>();
        return this;
    }

    private MemoryClientBuilder CompleteAsyncClient()
    {
        this.RequireOneEmbeddingGenerator();
        this.RequireOneVectorDb();
        this.AddSingleton<SearchClient, SearchClient>();
        this.AddSingleton<IPipelineOrchestrator, DistributedPipelineOrchestrator>();
        this.AddSingleton<DistributedPipelineOrchestrator, DistributedPipelineOrchestrator>();
        return this;
    }

    private MemoryClientBuilder AddSingleton<TService>(Func<IServiceProvider, TService> serviceFactory)
        where TService : class
    {
        this._appBuilder.Services.AddSingleton<TService>(serviceFactory);
        this._sharedServiceCollection?.AddSingleton<TService>(serviceFactory);
        return this;
    }

    private MemoryClientBuilder AddSingleton<TService>(TService implementationInstance)
        where TService : class
    {
        this._appBuilder.Services.AddSingleton<TService>(implementationInstance);
        this._sharedServiceCollection?.AddSingleton<TService>(implementationInstance);
        return this;
    }

    private MemoryClientBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        this._appBuilder.Services.AddSingleton<TService, TImplementation>();
        this._sharedServiceCollection?.AddSingleton<TService, TImplementation>();
        return this;
    }

    private T GetServiceConfig<T>(SemanticMemoryConfig cfg, string serviceName)
    {
        return cfg.GetServiceConfig<T>(this._appBuilder.Configuration, serviceName);
    }

    private void RequireOneEmbeddingGenerator()
    {
        if (this._embeddingGenerators.Count == 0 && !this._appBuilder.Services.Any(x => x.ServiceType == typeof(ITextEmbeddingGeneration)))
        {
            throw new ConfigurationException("Embedding generators not defined");
        }
    }

    private void RequireOneVectorDb()
    {
        if (this._vectorDbs.Count == 0 && !this._appBuilder.Services.Any(x => x.ServiceType == typeof(ISemanticMemoryVectorDb)))
        {
            throw new ConfigurationException("Vector DBs not defined");
        }
    }

    private void ReuseRetrievalEmbeddingGeneratorIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._embeddingGenerators.Count == 0 && this._appBuilder.Services.Any(x => x.ServiceType == typeof(ITextEmbeddingGeneration)))
        {
            this._embeddingGenerators.Add(serviceProvider.GetService<ITextEmbeddingGeneration>()
                                          ?? throw new ConfigurationException("Unable to build embedding generator"));
        }
    }

    private void ReuseRetrievalVectorDbIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._vectorDbs.Count == 0 && this._appBuilder.Services.Any(x => x.ServiceType == typeof(ISemanticMemoryVectorDb)))
        {
            this._vectorDbs.Add(serviceProvider.GetService<ISemanticMemoryVectorDb>()
                                ?? throw new ConfigurationException("Unable to build vector DB instance"));
        }
    }

    private ClientTypes GetBuildType()
    {
        var hasQueueFactory = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(QueueClientFactory)));
        var hasContentStorage = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(IContentStorage)));
        var hasMimeDetector = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(IMimeTypeDetection)));
        var hasEmbeddingGenerator = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(ITextEmbeddingGeneration)));
        var hasVectorDb = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(ISemanticMemoryVectorDb)));
        var hasTextGenerator = (this._appBuilder.Services.Any(x => x.ServiceType == typeof(ITextGeneration)));

        if (hasContentStorage && hasMimeDetector && hasEmbeddingGenerator && hasVectorDb && hasTextGenerator)
        {
            return hasQueueFactory ? ClientTypes.AsyncService : ClientTypes.SyncServerless;
        }

        var missing = new List<string>();
        if (!hasContentStorage) { missing.Add("Content storage"); }

        if (!hasMimeDetector) { missing.Add("MIME type detection"); }

        if (!hasEmbeddingGenerator) { missing.Add("Embedding generator"); }

        if (!hasVectorDb) { missing.Add("Vector DB"); }

        if (!hasTextGenerator) { missing.Add("Text generator"); }

        throw new ConfigurationException("Cannot build Memory client, some dependencies are not defined: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Basic helper for debugging issues in the memory builder
    /// </summary>
    private static void ShowException(Exception e)
    {
        if (e.StackTrace == null) { return; }

        string location = e.StackTrace.Trim()
            .Replace(" in ", "\n            in: ", StringComparison.OrdinalIgnoreCase)
            .Replace(":line ", "\n            line: ", StringComparison.OrdinalIgnoreCase);
        int pos = location.IndexOf("dotnet/", StringComparison.OrdinalIgnoreCase);
        if (pos > 0) { location = location.Substring(pos); }

        Console.Write($"## Error ##\n* Message:  {e.Message}\n* Type:     {e.GetType().Name} [{e.GetType().FullName}]\n* Location: {location}\n## ");
    }
}
