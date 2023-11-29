// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.AppBuilders;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory Builder interface.
/// Use this interface to add custom dependency injection extension methods.
/// </summary>
public interface IKernelMemoryBuilder
{
    /// <summary>
    /// Pool of service collections.
    /// Normally this consists of a single service collection used internally
    /// by the builder. However, one can share the hosting application service
    /// collection, for the builder to share internal dependencies out into
    /// the hosting application. In such case the pool contains two service
    /// collections, the memory builder's and the hosting application builder's.
    /// </summary>
    public ServiceCollectionPool Services { get; }

    /// <summary>
    /// Build the memory instance, using defaults and the provided dependencies
    /// and overrides. Depending on the dependencies provided, the resulting
    /// memory might use either an synchronous or asynchronous pipeline.
    /// </summary>
    public IKernelMemory Build();

    /// <summary>
    /// Build a specific type of memory instance, e.g. explicitly choosing
    /// between synchronous or asynchronous (queue based) pipeline.
    /// </summary>
    /// <typeparam name="T">Type of memory derived from IKernelMemory</typeparam>
    /// <returns>A memory instance</returns>
    public T Build<T>() where T : class, IKernelMemory;

    /// <summary>
    /// Setup the builder using settings from appsettings.json and appsettings.[ENV].json
    /// </summary>
    /// <param name="settingsDirectory">Directory where to look for configuration files</param>
    public IKernelMemoryBuilder FromAppSettings(string? settingsDirectory = null);

    /// <summary>
    /// Setup the builder using the provided configuration settings.
    /// </summary>
    /// <param name="config">Kernel Memory settings</param>
    /// <param name="servicesConfiguration">Host application settings</param>
    public IKernelMemoryBuilder FromConfiguration(KernelMemoryConfig config, IConfiguration servicesConfiguration);

    /// <summary>
    /// Add a singleton to the builder service collection pool.
    /// </summary>
    /// <param name="implementationInstance">Singleton instance</param>
    /// <typeparam name="TService">Singleton type</typeparam>
    public IKernelMemoryBuilder AddSingleton<TService>(TService implementationInstance)
        where TService : class;

    /// <summary>
    /// Add a singleton to the builder service collection pool.
    /// </summary>
    /// <typeparam name="TService">Singleton type, e.g. interface</typeparam>
    /// <typeparam name="TImplementation">Singleton implementation type</typeparam>
    public IKernelMemoryBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Remove the default pipeline handlers from the builder, allowing to specify
    /// a completely custom list of handlers.
    /// </summary>
    public IKernelMemoryBuilder WithoutDefaultHandlers();

    /// <summary>
    /// Add a memory DB to the list of DBs used during the ingestion.
    /// Note: it's possible writing to multiple DBs, all of them are used during the ingestion.
    /// </summary>
    /// <param name="service">Memory DB instance</param>
    public IKernelMemoryBuilder AddIngestionMemoryDb(IMemoryDb service);

    /// <summary>
    /// Add an embedding generator to the list of generators used during the ingestion.
    /// Note: it's possible using multiple generators, all of them are used during the ingestion.
    /// </summary>
    /// <param name="service">Embedding generator instance</param>
    public IKernelMemoryBuilder AddIngestionEmbeddingGenerator(ITextEmbeddingGeneration service);

    /// <summary>
    /// Return an instance of the pipeline orchestrator, usually required by custom handlers.
    /// </summary>
    public IPipelineOrchestrator GetOrchestrator();
}
