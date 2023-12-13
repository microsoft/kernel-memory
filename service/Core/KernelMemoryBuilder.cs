// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Llama;
using Microsoft.KernelMemory.AppBuilders;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.ContentStorage.AzureBlobs;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.DataFormats.Image.AzureAIDocIntel;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.MemoryStorage.Qdrant;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
using Microsoft.KernelMemory.Pipeline.Queue.AzureQueues;
using Microsoft.KernelMemory.Pipeline.Queue.DevTools;
using Microsoft.KernelMemory.Pipeline.Queue.RabbitMq;
using Microsoft.KernelMemory.Search;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder.
/// </summary>
public class KernelMemoryBuilder : IKernelMemoryBuilder
{
    private enum ClientTypes
    {
        Undefined,
        SyncServerless,
        AsyncService,
    }

    // appsettings.json root node name
    private const string ConfigRoot = "KernelMemory";

    // ASP.NET env var
    private const string AspnetEnv = "ASPNETCORE_ENVIRONMENT";

    // Proxy to the internal service collections, used to (optionally) inject dependencies
    // into the user application space
    private readonly ServiceCollectionPool _serviceCollections;

    // Services required to build the memory client class
    private readonly IServiceCollection _memoryServiceCollection;

    // Services of the host application, when hosting pipeline handlers, e.g. in service mode
    private readonly IServiceCollection? _hostServiceCollection;

    // Auxiliary collection used internally
    private readonly IServiceCollection _auxServiceCollection;

    // List of all the embedding generators to use during ingestion
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators = new();

    // List of all the memory DBs to use during ingestion
    private readonly List<IMemoryDb> _memoryDbs = new();

    // Normalized configuration
    private KernelMemoryConfig? _memoryConfiguration = null;

    // Content of appsettings.json, used to access dynamic data under "Services"
    private IConfiguration? _servicesConfiguration = null;

    /// <summary>
    /// Whether to register the default handlers. The list is hardcoded.
    /// Additional handlers can be configured as "default", see appsettings.json
    /// but they must be registered manually, including their dependencies
    /// if they depend on third party components.
    /// </summary>
    private bool _useDefaultHandlers = true;

    /// <summary>
    /// Proxy to the internal service collections, used to (optionally) inject
    /// dependencies into the user application space
    /// </summary>
    public ServiceCollectionPool Services
    {
        get => this._serviceCollections;
    }

    /// <summary>
    /// Create a new instance of the builder
    /// </summary>
    /// <param name="hostServiceCollection">Host application service collection, required
    /// when hosting the pipeline handlers. The builder will register in this collection
    /// all the dependencies required by the handlers, such as storage, embedding generators,
    /// AI dependencies, orchestrator classes, etc.</param>
    public KernelMemoryBuilder(IServiceCollection? hostServiceCollection = null)
    {
        this._memoryServiceCollection = new ServiceCollection();
        this._auxServiceCollection = new ServiceCollection();
        this._hostServiceCollection = hostServiceCollection;
        CopyServiceCollection(hostServiceCollection, this._memoryServiceCollection, this._auxServiceCollection);

        this._serviceCollections = new ServiceCollectionPool(this._memoryServiceCollection);
        this._serviceCollections.AddServiceCollection(this._auxServiceCollection);
        this._serviceCollections.AddServiceCollection(this._hostServiceCollection);

        // List of embedding generators and memory DBs used during the ingestion
        this._embeddingGenerators.Clear();
        this._memoryDbs.Clear();
        this.AddSingleton<List<ITextEmbeddingGenerator>>(this._embeddingGenerators);
        this.AddSingleton<List<IMemoryDb>>(this._memoryDbs);

        // Default configuration for tests and demos
        this.WithSimpleFileStorage(new SimpleFileStorageConfig { StorageType = FileSystemTypes.Volatile });
        this.WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Volatile });

        // Default dependencies, can be overridden
        this.WithDefaultMimeTypeDetection();
        this.WithDefaultPromptProvider();
    }

    ///<inheritdoc />
    public IKernelMemory Build()
    {
        var type = this.GetBuildType();
        switch (type)
        {
            case ClientTypes.SyncServerless:
                return this.BuildServerlessClient();

            case ClientTypes.AsyncService:
                return this.BuildAsyncClient();

            case ClientTypes.Undefined:
                throw new KernelMemoryException("Missing dependencies or insufficient configuration provided. " +
                                                "Try using With...() methods " +
                                                $"and other configuration methods before calling {nameof(this.Build)}(...)");

            default:
                throw new ArgumentOutOfRangeException($"Unsupported memory type '{type}'");
        }
    }

    ///<inheritdoc />
    public T Build<T>() where T : class, IKernelMemory
    {
        if (typeof(T) == typeof(MemoryServerless))
        {
            if (this.BuildServerlessClient() is not T result)
            {
                throw new InvalidOperationException($"Unable to instantiate '{typeof(MemoryServerless)}'. The instance is NULL.");
            }

            return result;
        }

        if (typeof(T) == typeof(MemoryService))
        {
            if (this.BuildAsyncClient() is not T result)
            {
                throw new InvalidOperationException($"Unable to instantiate '{typeof(MemoryService)}'. The instance is NULL.");
            }

            return result;
        }

        throw new KernelMemoryException($"The type of memory specified is not available, " +
                                        $"use either '{typeof(MemoryService)}' for the asynchronous memory with pipelines, " +
                                        $"or '{typeof(MemoryServerless)}' for the serverless synchronous memory client");
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder AddSingleton<TService>(TService implementationInstance)
        where TService : class
    {
        this.Services.AddSingleton<TService>(implementationInstance);
        return this;
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        this.Services.AddSingleton<TService, TImplementation>();
        return this;
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder FromAppSettings(string? settingsDirectory = null)
    {
        this._servicesConfiguration = ReadAppSettings(settingsDirectory);
        this._memoryConfiguration = this._servicesConfiguration.GetSection(ConfigRoot).Get<KernelMemoryConfig>()
                                    ?? throw new ConfigurationException($"Unable to parse configuration files. " +
                                                                        $"There should be a '{ConfigRoot}' root node, " +
                                                                        $"with data mapping to '{nameof(KernelMemoryConfig)}'");

        return this.FromConfiguration(this._memoryConfiguration, this._servicesConfiguration);
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder FromConfiguration(KernelMemoryConfig config, IConfiguration servicesConfiguration)
    {
        this._memoryConfiguration = config ?? throw new ConfigurationException("The given memory configuration is NULL");
        this._servicesConfiguration = servicesConfiguration ?? throw new ConfigurationException("The given service configuration is NULL");

        // Required by ctors expecting KernelMemoryConfig via DI
        this.AddSingleton(this._memoryConfiguration);

        this.WithDefaultMimeTypeDetection();

        // Ingestion queue
        if (string.Equals(config.DataIngestion.OrchestrationType, "Distributed", StringComparison.OrdinalIgnoreCase))
        {
            switch (config.DataIngestion.DistributedOrchestration.QueueType)
            {
                case string y when y.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase):
                    this.Services.AddAzureQueue(this.GetServiceConfig<AzureQueueConfig>(config, "AzureQueue"));
                    break;

                case string y when y.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase):
                    this.Services.AddRabbitMq(this.GetServiceConfig<RabbitMqConfig>(config, "RabbitMq"));
                    break;

                case string y when y.Equals("SimpleQueues", StringComparison.OrdinalIgnoreCase):
                    this.Services.AddSimpleQueues(this.GetServiceConfig<SimpleQueuesConfig>(config, "SimpleQueues"));
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
                this.Services.AddAzureBlobAsContentStorage(this.GetServiceConfig<AzureBlobsConfig>(config, "AzureBlobs"));
                break;

            case string x when x.Equals("SimpleFileStorage", StringComparison.OrdinalIgnoreCase):
                this.Services.AddSimpleFileStorageAsContentStorage(this.GetServiceConfig<SimpleFileStorageConfig>(config, "SimpleFileStorage"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomStorage()
                break;
        }

        // The ingestion embedding generators is a list of generators that the "gen_embeddings" handler uses,
        // to generate embeddings for each partition. While it's possible to use multiple generators (e.g. to compare embedding quality)
        // only one generator is used when searching by similarity, and the generator used for search is not in this list.
        // - config.DataIngestion.EmbeddingGeneratorTypes => list of generators, embeddings to generate and store in memory DB
        // - config.Retrieval.EmbeddingGeneratorType      => one embedding generator, used to search, and usually injected into Memory DB constructor
        // Note: use the aux service collection to avoid mixing ingestion and retrieval dependencies.

        // Note: using multiple embeddings is not fully supported yet and could cause write errors or incorrect search results
        if (config.DataIngestion.EmbeddingGeneratorTypes.Count > 1)
        {
            throw new NotSupportedException("Using multiple embedding generators is currently unsupported. " +
                                            "You may contact the team if this feature is required, or workaround this exception" +
                                            "using KernelMemoryBuilder methods explicitly.");
        }

        foreach (var type in config.DataIngestion.EmbeddingGeneratorTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
                case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                    var embeddingGenerator = this._auxServiceCollection.BuildServiceProvider().GetService<ITextEmbeddingGenerator>()
                                             ?? throw new ConfigurationException("Unable to build embedding generator");
                    this._embeddingGenerators.Add(embeddingGenerator);
                    this.ResetAuxServiceCollection();
                    break;
                }

                case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                    var embeddingGenerator = this._auxServiceCollection.BuildServiceProvider().GetService<ITextEmbeddingGenerator>()
                                             ?? throw new ConfigurationException("Unable to build embedding generator");
                    this._embeddingGenerators.Add(embeddingGenerator);
                    this.ResetAuxServiceCollection();
                    break;
                }

                default:
                    // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                    break;
            }
        }

        // Search settings
        this.WithSearchClientConfig(config.Retrieval.SearchClient);

        // Retrieval embeddings - ITextEmbeddingGeneration interface
        switch (config.Retrieval.EmbeddingGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIEmbedding", StringComparison.OrdinalIgnoreCase):
                this.Services.AddAzureOpenAIEmbeddingGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIEmbedding"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this.Services.AddOpenAITextEmbeddingGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomEmbeddingGeneration()
                break;
        }

        // The ingestion Memory DBs is a list of DBs where handlers write records to. While it's possible
        // to write to multiple DBs, e.g. for replication purpose, there is only one Memory DB used to
        // read/search, and it doesn't come from this list. See "config.Retrieval.MemoryDbType".
        // Note: use the aux service collection to avoid mixing ingestion and retrieval dependencies.
        foreach (var type in config.DataIngestion.MemoryDbTypes)
        {
            switch (type)
            {
                case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddAzureAISearchAsMemoryDb(this.GetServiceConfig<AzureAISearchConfig>(config, "AzureAISearch"));
                    var serviceProvider = this._auxServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<IMemoryDb>() ?? throw new ConfigurationException("Unable to build ingestion memory DB");
                    this._memoryDbs.Add(service);
                    this.ResetAuxServiceCollection();
                    break;
                }

                case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddQdrantAsMemoryDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                    var serviceProvider = this._auxServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<IMemoryDb>() ?? throw new ConfigurationException("Unable to build ingestion memory DB");
                    this._memoryDbs.Add(service);
                    this.ResetAuxServiceCollection();
                    break;
                }

                case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddSimpleVectorDbAsMemoryDb(this.GetServiceConfig<SimpleVectorDbConfig>(config, "SimpleVectorDb"));
                    var serviceProvider = this._auxServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<IMemoryDb>() ?? throw new ConfigurationException("Unable to build ingestion memory DB");
                    this._memoryDbs.Add(service);
                    this.ResetAuxServiceCollection();
                    break;
                }

                case string x when x.Equals("SimpleTextDb", StringComparison.OrdinalIgnoreCase):
                {
                    this._auxServiceCollection.AddSimpleTextDbAsMemoryDb(this.GetServiceConfig<SimpleTextDbConfig>(config, "SimpleTextDb"));
                    var serviceProvider = this._auxServiceCollection.BuildServiceProvider();
                    var service = serviceProvider.GetService<IMemoryDb>() ?? throw new ConfigurationException("Unable to build ingestion memory DB");
                    this._memoryDbs.Add(service);
                    this.ResetAuxServiceCollection();
                    break;
                }

                default:
                    // NOOP - allow custom implementations, via WithCustomMemoryDb()
                    break;
            }
        }

        // Retrieval Memory DB - IMemoryDb interface
        switch (config.Retrieval.MemoryDbType)
        {
            case string x when x.Equals("AzureAISearch", StringComparison.OrdinalIgnoreCase):
                this.Services.AddAzureAISearchAsMemoryDb(this.GetServiceConfig<AzureAISearchConfig>(config, "AzureAISearch"));
                break;

            case string x when x.Equals("Qdrant", StringComparison.OrdinalIgnoreCase):
                this.Services.AddQdrantAsMemoryDb(this.GetServiceConfig<QdrantConfig>(config, "Qdrant"));
                break;

            case string x when x.Equals("SimpleVectorDb", StringComparison.OrdinalIgnoreCase):
                this.Services.AddSimpleVectorDbAsMemoryDb(this.GetServiceConfig<SimpleVectorDbConfig>(config, "SimpleVectorDb"));
                break;

            case string x when x.Equals("SimpleTextDb", StringComparison.OrdinalIgnoreCase):
                this.Services.AddSimpleTextDbAsMemoryDb(this.GetServiceConfig<SimpleTextDbConfig>(config, "SimpleTextDb"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomMemoryDb()
                break;
        }

        // Text generation
        switch (config.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                this.Services.AddAzureOpenAITextGeneration(this.GetServiceConfig<AzureOpenAIConfig>(config, "AzureOpenAIText"));
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                this.Services.AddOpenAITextGeneration(this.GetServiceConfig<OpenAIConfig>(config, "OpenAI"));
                break;

            case string x when x.Equals("LlamaSharp", StringComparison.OrdinalIgnoreCase):
                this.Services.AddLlamaTextGeneration(this.GetServiceConfig<LlamaSharpConfig>(config, "LlamaSharp"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomTextGeneration()
                break;
        }

        // Image OCR
        switch (config.DataIngestion.ImageOcrType)
        {
            case string y when string.IsNullOrWhiteSpace(y):
            case string x when x.Equals("None", StringComparison.OrdinalIgnoreCase):
                break;

            case string x when x.Equals("AzureAIDocIntel", StringComparison.OrdinalIgnoreCase):
                this.Services.AddAzureAIDocIntel(this.GetServiceConfig<AzureAIDocIntelConfig>(config, "AzureAIDocIntel"));
                break;

            default:
                // NOOP - allow custom implementations, via WithCustomImageOCR()
                break;
        }

        return this;
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder WithoutDefaultHandlers()
    {
        this._useDefaultHandlers = false;
        return this;
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder AddIngestionMemoryDb(IMemoryDb service)
    {
        this._memoryDbs.Add(service);
        return this;
    }

    ///<inheritdoc />
    public IKernelMemoryBuilder AddIngestionEmbeddingGenerator(ITextEmbeddingGenerator service)
    {
        this._embeddingGenerators.Add(service);
        return this;
    }

    ///<inheritdoc />
    public IPipelineOrchestrator GetOrchestrator()
    {
        var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
        return serviceProvider.GetService<IPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
    }

    #region internals

    private void ResetAuxServiceCollection()
    {
        this._auxServiceCollection.Clear();
        foreach (ServiceDescriptor d in this._memoryServiceCollection)
        {
            this._auxServiceCollection.Add(d);
        }
    }

    private static void CopyServiceCollection(
        IServiceCollection? source,
        IServiceCollection destination1,
        IServiceCollection? destination2 = null)
    {
        if (source == null) { return; }

        foreach (ServiceDescriptor d in source)
        {
            destination1.Add(d);
            destination2?.Add(d);
        }
    }

    private MemoryServerless BuildServerlessClient()
    {
        try
        {
            // Add handlers to DI service collection
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

                this._memoryServiceCollection.AddTransient<SaveRecordsHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<SaveRecordsHandler>(serviceProvider, "save_records"));

                this._memoryServiceCollection.AddTransient<DeleteDocumentHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<DeleteDocumentHandler>(serviceProvider, Constants.DeleteDocumentPipelineStepName));

                this._memoryServiceCollection.AddTransient<DeleteIndexHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<DeleteIndexHandler>(serviceProvider, Constants.DeleteIndexPipelineStepName));
            }

            var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();

            this.CompleteServerlessClient(serviceProvider);

            // In case the user didn't set the embedding generator and memory DB to use for ingestion, use the values set for retrieval
            this.ReuseRetrievalEmbeddingGeneratorIfNecessary(serviceProvider);
            this.ReuseRetrievalMemoryDbIfNecessary(serviceProvider);

            // Recreate the service provider, in order to have the latest dependencies just configured
            serviceProvider = this._memoryServiceCollection.BuildServiceProvider();

            var orchestrator = serviceProvider.GetService<InProcessPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
            var searchClient = serviceProvider.GetService<ISearchClient>() ?? throw new ConfigurationException("Unable to build search client");

            this.CheckForMissingDependencies();

            var memoryClientInstance = new MemoryServerless(orchestrator, searchClient);

            // Load handlers in the memory client
            if (this._useDefaultHandlers)
            {
                memoryClientInstance.AddHandler(serviceProvider.GetService<TextExtractionHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(TextExtractionHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<TextPartitioningHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(TextPartitioningHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<SummarizationHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(SummarizationHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<GenerateEmbeddingsHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(GenerateEmbeddingsHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<SaveRecordsHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(SaveRecordsHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<DeleteDocumentHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(DeleteDocumentHandler)));
                memoryClientInstance.AddHandler(serviceProvider.GetService<DeleteIndexHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(DeleteIndexHandler)));
            }

            return memoryClientInstance;
        }
        catch (Exception e)
        {
            ShowException(e);
            throw;
        }
    }

    private MemoryService BuildAsyncClient()
    {
        var serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
        this.CompleteAsyncClient(serviceProvider);

        // In case the user didn't set the embedding generator and memory DB to use for ingestion, use the values set for retrieval
        this.ReuseRetrievalEmbeddingGeneratorIfNecessary(serviceProvider);
        this.ReuseRetrievalMemoryDbIfNecessary(serviceProvider);

        // Recreate the service provider, in order to have the latest dependencies just configured
        serviceProvider = this._memoryServiceCollection.BuildServiceProvider();

        var orchestrator = serviceProvider.GetService<DistributedPipelineOrchestrator>() ?? throw new ConfigurationException("Unable to build orchestrator");
        var searchClient = serviceProvider.GetService<ISearchClient>() ?? throw new ConfigurationException("Unable to build search client");

        if (this._useDefaultHandlers)
        {
            if (this._hostServiceCollection == null)
            {
                const string ClassName = nameof(KernelMemoryBuilder);
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
            this._hostServiceCollection.AddHandlerAsHostedService<SaveRecordsHandler>("save_records");
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.DeleteDocumentPipelineStepName);
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.DeleteIndexPipelineStepName);
        }

        this.CheckForMissingDependencies();

        return new MemoryService(orchestrator, searchClient);
    }

    private KernelMemoryBuilder CompleteServerlessClient(ServiceProvider serviceProvider)
    {
        this.UseDefaultSearchClientIfNecessary(serviceProvider);
        this.AddSingleton<IPipelineOrchestrator, InProcessPipelineOrchestrator>();
        this.AddSingleton<InProcessPipelineOrchestrator, InProcessPipelineOrchestrator>();
        return this;
    }

    private KernelMemoryBuilder CompleteAsyncClient(ServiceProvider serviceProvider)
    {
        this.UseDefaultSearchClientIfNecessary(serviceProvider);
        this.AddSingleton<IPipelineOrchestrator, DistributedPipelineOrchestrator>();
        this.AddSingleton<DistributedPipelineOrchestrator, DistributedPipelineOrchestrator>();
        return this;
    }

    private void CheckForMissingDependencies()
    {
        this.RequireEmbeddingGenerator();
        this.RequireOneMemoryDbForIngestion();
        this.RequireOneMemoryDbForRetrieval();
    }

    private T GetServiceConfig<T>(KernelMemoryConfig cfg, string serviceName)
    {
        if (this._servicesConfiguration == null)
        {
            throw new ConfigurationException("Services configuration is NULL");
        }

        return cfg.GetServiceConfig<T>(this._servicesConfiguration, serviceName);
    }

    private void RequireEmbeddingGenerator()
    {
        if (this.IsEmbeddingGeneratorEnabled() && this._embeddingGenerators.Count == 0)
        {
            throw new ConfigurationException("No embedding generators configured for memory ingestion. Check 'EmbeddingGeneratorTypes' setting.");
        }
    }

    private void RequireOneMemoryDbForIngestion()
    {
        if (this._memoryDbs.Count == 0)
        {
            throw new ConfigurationException("Memory DBs for ingestion not configured");
        }
    }

    private void RequireOneMemoryDbForRetrieval()
    {
        if (!this._memoryServiceCollection.HasService<IMemoryDb>())
        {
            throw new ConfigurationException("Memory DBs for retrieval not configured");
        }
    }

    private void UseDefaultSearchClientIfNecessary(ServiceProvider serviceProvider)
    {
        if (!this._memoryServiceCollection.HasService<ISearchClient>())
        {
            this.WithDefaultSearchClient(serviceProvider.GetService<SearchClientConfig>());
        }
    }

    private void ReuseRetrievalEmbeddingGeneratorIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._embeddingGenerators.Count == 0 && this._memoryServiceCollection.HasService<ITextEmbeddingGenerator>())
        {
            this._embeddingGenerators.Add(serviceProvider.GetService<ITextEmbeddingGenerator>()
                                          ?? throw new ConfigurationException("Unable to build embedding generator"));
        }
    }

    private void ReuseRetrievalMemoryDbIfNecessary(IServiceProvider serviceProvider)
    {
        if (this._memoryDbs.Count == 0 && this._memoryServiceCollection.HasService<IMemoryDb>())
        {
            this._memoryDbs.Add(serviceProvider.GetService<IMemoryDb>()
                                ?? throw new ConfigurationException("Unable to build memory DB instance"));
        }
    }

    private bool IsEmbeddingGeneratorEnabled()
    {
        return this._memoryConfiguration is null or { DataIngestion.EmbeddingGenerationEnabled: true };
    }

    private ClientTypes GetBuildType()
    {
        var hasQueueFactory = (this._memoryServiceCollection.HasService<QueueClientFactory>());
        var hasContentStorage = (this._memoryServiceCollection.HasService<IContentStorage>());
        var hasMimeDetector = (this._memoryServiceCollection.HasService<IMimeTypeDetection>());
        var hasEmbeddingGenerator = (this._memoryServiceCollection.HasService<IMimeTypeDetection>());
        var hasMemoryDb = (this._memoryServiceCollection.HasService<IMemoryDb>());
        var hasTextGenerator = (this._memoryServiceCollection.HasService<ITextGenerator>());

        if (hasContentStorage && hasMimeDetector && hasEmbeddingGenerator && hasMemoryDb && hasTextGenerator)
        {
            return hasQueueFactory ? ClientTypes.AsyncService : ClientTypes.SyncServerless;
        }

        var missing = new List<string>();
        if (!hasContentStorage) { missing.Add("Content storage"); }

        if (!hasMimeDetector) { missing.Add("MIME type detection"); }

        if (!hasEmbeddingGenerator) { missing.Add("Embedding generator"); }

        if (!hasMemoryDb) { missing.Add("Memory DB"); }

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

    private static IConfiguration ReadAppSettings(string? settingsDirectory)
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

    #endregion
}
