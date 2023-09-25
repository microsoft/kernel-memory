﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.SemanticMemory.Handlers;
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
    private enum ClientTypes
    {
        Undefined,
        SyncServerless,
        AsyncService,
    }

    // appsettings.json root node name
    private const string ConfigRoot = "SemanticMemory";

    // ASP.NET env var
    private const string AspnetEnv = "ASPNETCORE_ENVIRONMENT";

    // Services required to build the memory client class
    private readonly IServiceCollection _memoryServiceCollection;

    // Services of the host application, when hosting pipeline handlers, e.g. in service mode
    private readonly IServiceCollection? _hostServiceCollection;

    // List of all the embedding generators to use during ingestion
    private readonly List<ITextEmbeddingGeneration> _embeddingGenerators = new();

    // List of all the vector DBs to use during ingestion
    private readonly List<ISemanticMemoryVectorDb> _vectorDbs = new();

    // Normalized configuration
    private SemanticMemoryConfig? _memoryConfiguration = null;

    // Content of appsettings.json, used to access dynamic data under "Services"
    private IConfiguration? _servicesConfiguration = null;

    /// <summary>
    /// Whether to register the default handlers. The list is hardcoded.
    /// Additional handlers can be configured as "default", see appsettings.json
    /// but they must be registered manually, including their dependencies
    /// if they depend on third party components. 
    /// </summary>
    private bool _useDefaultHandlers = true;

    public IServiceCollection Services
    {
        get => this._memoryServiceCollection;
    }

    /// <summary>
    /// Create a new instance of the builder
    /// </summary>
    /// <param name="hostServiceCollection">Host application service collection, required
    /// when hosting the pipeline handlers. The builder will register in this collection
    /// all the dependencies required by the handlers, such as storage, embedding generators,
    /// AI dependencies, orchestrator classes, etc.</param>
    public MemoryClientBuilder(IServiceCollection? hostServiceCollection = null)
    {
        this._memoryServiceCollection = new ServiceCollection();
        this._hostServiceCollection = hostServiceCollection;

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

    public MemoryClientBuilder WithoutDefaultHandlers()
    {
        this._useDefaultHandlers = false;
        return this;
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

    public MemoryClientBuilder FromAppSettings(string? settingsDirectory = null)
    {
        this._servicesConfiguration = this.ReadAppSettings(settingsDirectory);
        this._memoryConfiguration = this._servicesConfiguration.GetSection(ConfigRoot).Get<SemanticMemoryConfig>()
                                    ?? throw new ConfigurationException($"Unable to parse configuration files. " +
                                                                        $"There should be a '{ConfigRoot}' root node, " +
                                                                        $"with data mapping to '{nameof(SemanticMemoryConfig)}'");

        return this.FromConfiguration(this._memoryConfiguration, this._servicesConfiguration);
    }

    public MemoryClientBuilder FromConfiguration(SemanticMemoryConfig config, IConfiguration servicesConfiguration)
    {
        this._memoryConfiguration = config ?? throw new ConfigurationException("The given memory configuration is NULL");
        this._servicesConfiguration = servicesConfiguration ?? throw new ConfigurationException("The given service configuration is NULL");

        this.WithDefaultMimeTypeDetection();

        // Ingestion queue
        if (string.Equals(config.DataIngestion.OrchestrationType, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            switch (config.DataIngestion.DistributedOrchestration.QueueType)
            {
                case string y when y.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                    this._memoryServiceCollection.AddAzureQueue(this.GetServiceConfig<AzureQueueConfig>(config, "AzureQueue"));
                    this._hostServiceCollection?.AddAzureQueue(this.GetServiceConfig<AzureQueueConfig>(config, "AzureQueue"));
                    break;

                case string y when y.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    this._memoryServiceCollection.AddRabbitMq(this.GetServiceConfig<RabbitMqConfig>(config, "RabbitMq"));
                    this._hostServiceCollection?.AddRabbitMq(this.GetServiceConfig<RabbitMqConfig>(config, "RabbitMq"));
                    break;

                case string y when y.Equals("SimpleQueues", StringComparison.OrdinalIgnoreCase):
                    this._memoryServiceCollection.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>(config, "SimpleQueues"));
                    this._hostServiceCollection?.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>(config, "SimpleQueues"));
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
                this._memoryServiceCollection.AddAzureBlobAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>(config, "AzureBlobs"));
                this._hostServiceCollection?.AddAzureBlobAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>(config, "AzureBlobs"));
                break;

            case string x when x.Equals("SimpleFileStorage", StringComparison.OrdinalIgnoreCase):
                this._memoryServiceCollection.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>(config, "SimpleFileStorage"));
                this._hostServiceCollection?.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>(config, "SimpleFileStorage"));
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
                this._memoryServiceCollection.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                this._hostServiceCollection?.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this._memoryServiceCollection.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                this._hostServiceCollection?.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
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
                    this._memoryServiceCollection.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                    var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<ITextEmbeddingGeneration>() ?? throw new ConfigurationException("Unable to build embedding generator");
                    this._embeddingGenerators.Add(service);

                    break;
                }

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                {
                    this._memoryServiceCollection.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                    var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<ITextEmbeddingGeneration>() ?? throw new ConfigurationException("Unable to build embedding generator");
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
                this._memoryServiceCollection.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                this._hostServiceCollection?.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                break;

            case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                this._memoryServiceCollection.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                this._hostServiceCollection?.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                break;

            case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                this._memoryServiceCollection.AddSimpleVectorDbAsVectorDb(this.GetServiceConfig<SimpleVectorDbConfig>(config, "SimpleVectorDb"));
                this._hostServiceCollection?.AddSimpleVectorDbAsVectorDb(this.GetServiceConfig<SimpleVectorDbConfig>(config, "SimpleVectorDb"));
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
                    this._memoryServiceCollection.AddAzureCognitiveSearchAsVectorDb(this.GetServiceConfig<AzureCognitiveSearchConfig>(config, "AzureCognitiveSearch"));
                    var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<ISemanticMemoryVectorDb>() ?? throw new ConfigurationException("Unable to build ingestion vector DB");
                    this._vectorDbs.Add(service);
                    break;
                }

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                {
                    this._memoryServiceCollection.AddQdrantAsVectorDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                    var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<ISemanticMemoryVectorDb>() ?? throw new ConfigurationException("Unable to build ingestion vector DB");
                    this._vectorDbs.Add(service);
                    break;
                }

                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                {
                    this._memoryServiceCollection.AddSimpleVectorDbAsVectorDb(this.GetServiceConfig<SimpleVectorDbConfig>(config, "SimpleVectorDb"));
                    var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<ISemanticMemoryVectorDb>() ?? throw new ConfigurationException("Unable to build ingestion vector DB");
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
                this._memoryServiceCollection.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIText"));
                this._hostServiceCollection?.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIText"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this._memoryServiceCollection.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                this._hostServiceCollection?.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
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
                this._memoryServiceCollection.AddAzureFormRecognizer(this.GetServiceConfig<AzureFormRecognizerConfig>(config, "AzureFormRecognizer"));
                this._hostServiceCollection?.AddAzureFormRecognizer(this.GetServiceConfig<AzureFormRecognizerConfig>(config, "AzureFormRecognizer"));
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
        var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
        return serviceProvider.GetService<IPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
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

            if (this._useDefaultHandlers)
            {
                this._memoryServiceCollection.AddTransient<TextExtractionHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<TextExtractionHandler>(serviceProvider, "extract"));

                this._memoryServiceCollection.AddTransient<TextPartitioningHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<TextPartitioningHandler>(serviceProvider, "partition"));

                this._memoryServiceCollection.AddTransient<SummarizationHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<SummarizationHandler>(serviceProvider, "summarize"));

                this._memoryServiceCollection.AddTransient<GenerateEmbeddingsHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<GenerateEmbeddingsHandler>(serviceProvider, "gen_embeddings"));

                this._memoryServiceCollection.AddTransient<SaveEmbeddingsHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<SaveEmbeddingsHandler>(serviceProvider, "save_embeddings"));

                this._memoryServiceCollection.AddTransient<DeleteDocumentHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<DeleteDocumentHandler>(serviceProvider, Constants.DeleteDocumentPipelineStepName));
            }

            var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();

            // In case the user didn't set the embedding generator and vector DB to use for ingestion, use the values set for retrieval 
            this.ReuseRetrievalEmbeddingGeneratorIfNecessary(serviceProvider);
            this.ReuseRetrievalVectorDbIfNecessary(serviceProvider);

            var orchestrator = serviceProvider.GetService<InProcessPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
            var searchClient = serviceProvider.GetService<SearchClient>() ?? throw new ConfigurationException("Unable to build search client");

            var memoryClientInstance = new Memory(orchestrator, searchClient);

            if (this._useDefaultHandlers)
            {
                memoryClientInstance.AddHandler(serviceProvider.GetService<TextExtractionHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(TextExtractionHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<TextPartitioningHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(TextPartitioningHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<SummarizationHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(SummarizationHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<GenerateEmbeddingsHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(GenerateEmbeddingsHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<SaveEmbeddingsHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(SaveEmbeddingsHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<DeleteDocumentHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(DeleteDocumentHandler)));
            }

            return memoryClientInstance;
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
        var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();

        // In case the user didn't set the embedding generator and vector DB to use for ingestion, use the values set for retrieval 
        this.ReuseRetrievalEmbeddingGeneratorIfNecessary(serviceProvider);
        this.ReuseRetrievalVectorDbIfNecessary(serviceProvider);

        var orchestrator = serviceProvider.GetService<DistributedPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
        var searchClient = serviceProvider.GetService<SearchClient>() ?? throw new ConfigurationException("Unable to build search client");

        if (this._useDefaultHandlers)
        {
            if (this._hostServiceCollection == null)
            {
                const string ClassName = nameof(MemoryClientBuilder);
                const string MethodName = nameof(this.WithoutDefaultHandlers);
                throw new ConfigurationException("Service collection not available, unable to register default handlers. " +
                                                 $"If you'd like using the default handlers use `new {ClassName}(<your service collection provider>)`, " +
                                                 $"otherwise use `{ClassName}(...).{MethodName}()` to manage the list of handlers manually.");
            }

            // Handlers - Register these handlers to run as hosted services in the caller app.
            // At start each hosted handler calls IPipelineOrchestrator.AddHandlerAsync() to register in the orchestrator.
            this._hostServiceCollection.AddHandlerAsHostedService<TextExtractionHandler>("extract");
            this._hostServiceCollection.AddHandlerAsHostedService<SummarizationHandler>("summarize");
            this._hostServiceCollection.AddHandlerAsHostedService<TextPartitioningHandler>("partition");
            this._hostServiceCollection.AddHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
            this._hostServiceCollection.AddHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.DeleteDocumentPipelineStepName);
        }

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

    private MemoryClientBuilder AddSingleton<TService>(TService implementationInstance)
        where TService : class
    {
        this._memoryServiceCollection.AddSingleton<TService>(implementationInstance);
        this._hostServiceCollection?.AddSingleton<TService>(implementationInstance);
        return this;
    }

    private MemoryClientBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        this._memoryServiceCollection.AddSingleton<TService, TImplementation>();
        this._hostServiceCollection?.AddSingleton<TService, TImplementation>();
        return this;
    }

    private T GetServiceConfig<T>(SemanticMemoryConfig cfg, string serviceName)
    {
        if (this._servicesConfiguration == null)
        {
            throw new ConfigurationException("Services configuration is NULL");
        }

        return cfg.GetServiceConfig<T>(this._servicesConfiguration, serviceName);
    }

    private void RequireOneEmbeddingGenerator()
    {
        if (this._embeddingGenerators.Count == 0 && this._memoryServiceCollection.All(x => x.ServiceType != typeof(ITextEmbeddingGeneration)))
        {
            throw new ConfigurationException("Embedding generators not defined");
        }
    }

    private void RequireOneVectorDb()
    {
        if (this._vectorDbs.Count == 0 && this._memoryServiceCollection.All(x => x.ServiceType != typeof(ISemanticMemoryVectorDb)))
        {
            throw new ConfigurationException("Vector DBs not defined");
        }
    }

    private void ReuseRetrievalEmbeddingGeneratorIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._embeddingGenerators.Count == 0 && this._memoryServiceCollection.Any(x => x.ServiceType == typeof(ITextEmbeddingGeneration)))
        {
            this._embeddingGenerators.Add(serviceProvider.GetService<ITextEmbeddingGeneration>()
                                          ?? throw new ConfigurationException("Unable to build embedding generator"));
        }
    }

    private void ReuseRetrievalVectorDbIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._vectorDbs.Count == 0 && this._memoryServiceCollection.Any(x => x.ServiceType == typeof(ISemanticMemoryVectorDb)))
        {
            this._vectorDbs.Add(serviceProvider.GetService<ISemanticMemoryVectorDb>()
                                ?? throw new ConfigurationException("Unable to build vector DB instance"));
        }
    }

    private ClientTypes GetBuildType()
    {
        var hasQueueFactory = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(QueueClientFactory)));
        var hasContentStorage = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(IContentStorage)));
        var hasMimeDetector = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(IMimeTypeDetection)));
        var hasEmbeddingGenerator = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(ITextEmbeddingGeneration)));
        var hasVectorDb = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(ISemanticMemoryVectorDb)));
        var hasTextGenerator = (this._memoryServiceCollection.Any(x => x.ServiceType == typeof(ITextGeneration)));

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

    private IConfiguration ReadAppSettings(string? settingsDirectory)
    {
        if (settingsDirectory == null)
        {
            settingsDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
        }

        var env = Environment.GetEnvironmentVariable(AspnetEnv)?.ToLowerInvariant() ?? string.Empty;
        var builder = new ConfigurationBuilder();

        builder.SetBasePath(settingsDirectory);

        var main = Path.Join(settingsDirectory, "appsettings.json");
        if (File.Exists(main))
        {
            builder.AddJsonFile(main, optional: false);
        }
        else
        {
            throw new ConfigurationException($"appsettings.json not found. Directory: {settingsDirectory}");
        }

        if (env.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            var f1 = Path.Join(settingsDirectory, "appsettings.development.json");
            var f2 = Path.Join(settingsDirectory, "appsettings.Development.json");
            if (File.Exists(f1))
            {
                builder.AddJsonFile(f1, optional: false);
            }
            else if (File.Exists(f2))
            {
                builder.AddJsonFile(f2, optional: false);
            }
        }

        if (env.Equals("production", StringComparison.OrdinalIgnoreCase))
        {
            var f1 = Path.Join(settingsDirectory, "appsettings.production.json");
            var f2 = Path.Join(settingsDirectory, "appsettings.Production.json");
            if (File.Exists(f1))
            {
                builder.AddJsonFile(f1, optional: false);
            }
            else if (File.Exists(f2))
            {
                builder.AddJsonFile(f2, optional: false);
            }
        }

        // Support for environment variables overriding the config files
        builder.AddEnvironmentVariables();

        // Support for user secrets. Secret Manager doesn't encrypt the stored secrets and
        // shouldn't be treated as a trusted store. It's for development purposes only.
        // see: https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows#secret-manager
        if (env.Equals("development", StringComparison.OrdinalIgnoreCase))
        {
            // GetEntryAssembly method can return null if this library is loaded
            // from an unmanaged application, in which case UserSecrets are not supported.
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                builder.AddUserSecrets(entryAssembly, optional: true);
            }
        }

        return builder.Build();
    }
}
