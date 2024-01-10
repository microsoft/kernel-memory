// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AppBuilders;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Pipeline.Queue;
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

    // Proxy to the internal service collections, used to (optionally) inject dependencies
    // into the user application space
    private readonly ServiceCollectionPool _serviceCollections;

    // Services required to build the memory client class
    private readonly IServiceCollection _memoryServiceCollection;

    // Services of the host application, when hosting pipeline handlers, e.g. in service mode
    private readonly IServiceCollection? _hostServiceCollection;

    // List of all the embedding generators to use during ingestion
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators = new();

    // List of all the memory DBs to use during ingestion
    private readonly List<IMemoryDb> _memoryDbs = new();

    // Normalized configuration
    private readonly KernelMemoryConfig? _memoryConfiguration = null;

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
        this._hostServiceCollection = hostServiceCollection;
        CopyServiceCollection(hostServiceCollection, this._memoryServiceCollection);

        // Important: this._memoryServiceCollection is the primary service collection
        this._serviceCollections = new ServiceCollectionPool(this._memoryServiceCollection);
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
                    => ActivatorUtilities.CreateInstance<DeleteDocumentHandler>(serviceProvider, Constants.PipelineStepsDeleteDocument));

                this._memoryServiceCollection.AddTransient<DeleteIndexHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<DeleteIndexHandler>(serviceProvider, Constants.PipelineStepsDeleteIndex));

                this._memoryServiceCollection.AddTransient<DeleteGeneratedFilesHandler>(serviceProvider
                    => ActivatorUtilities.CreateInstance<DeleteGeneratedFilesHandler>(serviceProvider, Constants.PipelineStepsDeleteGeneratedFiles));
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
                memoryClientInstance.AddHandler(serviceProvider.GetService<DeleteGeneratedFilesHandler>() ?? throw new ConfigurationException("Unable to build " + nameof(DeleteGeneratedFilesHandler)));
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
        ServiceProvider serviceProvider = this._memoryServiceCollection.BuildServiceProvider();
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
                throw new ConfigurationException("Host service collection not available: unable to register default handlers. " +
                                                 $"If you'd like using the default handlers use `new {ClassName}(<your service collection provider>)`, " +
                                                 $"otherwise use `new {ClassName}(...).{MethodName}()` to manage the list of handlers manually.");
            }

            // Handlers - Register these handlers to run as hosted services in the caller app.
            // At start each hosted handler calls IPipelineOrchestrator.AddHandlerAsync() to register in the orchestrator.
            this._hostServiceCollection.AddHandlerAsHostedService<TextExtractionHandler>(Constants.PipelineStepsExtract);
            this._hostServiceCollection.AddHandlerAsHostedService<TextPartitioningHandler>(Constants.PipelineStepsPartition);
            this._hostServiceCollection.AddHandlerAsHostedService<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
            this._hostServiceCollection.AddHandlerAsHostedService<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
            this._hostServiceCollection.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
            this._hostServiceCollection.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);
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
        var hasEmbeddingGenerator = (this._memoryServiceCollection.HasService<ITextEmbeddingGenerator>());
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

    #endregion
}
