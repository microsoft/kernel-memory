// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.MemoryStorage;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticMemory.Core.Pipeline.Queue;
using Microsoft.SemanticMemory.Core.Search;

namespace Microsoft.SemanticMemory.Core.AppBuilders;

#pragma warning disable CA1724 // The name conflicts with MSExtensions
public static class DependencyInjection
{
    public static void ConfigureLogger(this ILoggingBuilder builder)
    {
        // Note: log level is set via config file

        builder.ClearProviders();
        builder.AddConsole();
    }

    public static SemanticMemoryConfig UseConfiguration(this IServiceCollection services, ConfigurationManager mgr)
    {
        // Populate the global config from the "SemanticMemory" key
        SemanticMemoryConfig config = mgr.GetSection("SemanticMemory").Get<SemanticMemoryConfig>()!;

        // Copy the RAW handlers configuration from "SemanticMemory.Handlers". Binding is done later by each handler.
        // TODO: find a solution to move the binding logic here, simplifying the configuration classes.
        IConfigurationSection handlersConfigSection = mgr.GetSection("SemanticMemory").GetSection("Handlers");
        config.Handlers = new();
        foreach (IConfigurationSection bar in handlersConfigSection.GetChildren())
        {
            config.Handlers[bar.Key] = handlersConfigSection.GetSection(bar.Key);
        }

        services.AddSingleton(config);
        return config;
    }

    public static void UseSearchClient(this IServiceCollection services, SemanticMemoryConfig config)
    {
        services.UseContentStorage(config);
        services.UseOrchestrator(config);

        ISemanticMemoryVectorDb vectorDb;
        ITextEmbeddingGeneration embeddingGenerator;
        IKernel kernel;

        // TODO: decouple this file from the various options
        switch (config.Search.GetVectorDbConfig())
        {
            case AzureCognitiveSearchConfig cfg:
                vectorDb = new AzureCognitiveSearchMemory(
                    endpoint: cfg.Endpoint,
                    apiKey: cfg.APIKey,
                    indexPrefix: cfg.VectorIndexPrefix);
                break;

            default:
                throw new SemanticMemoryException(
                    $"Unknown/unsupported vector DB '{config.Search.VectorDb.GetType().FullName}'");
        }

        // TODO: decouple this file from the various options
        switch (config.Search.GetEmbeddingGeneratorConfig())
        {
            case AzureOpenAIConfig cfg:
                embeddingGenerator = new AzureTextEmbeddingGeneration(
                    modelId: cfg.Deployment,
                    endpoint: cfg.Endpoint,
                    apiKey: cfg.APIKey,
                    logger: DefaultLogger<AzureTextEmbeddingGeneration>.Instance);
                break;

            case OpenAIConfig cfg:
                embeddingGenerator = new OpenAITextEmbeddingGeneration(
                    modelId: cfg.Model,
                    apiKey: cfg.APIKey,
                    organization: cfg.OrgId,
                    logger: DefaultLogger<AzureTextEmbeddingGeneration>.Instance);
                break;

            default:
                throw new SemanticMemoryException(
                    $"Unknown/unsupported embedding generator '{config.Search.EmbeddingGenerator.GetType().FullName}'");
        }

        // TODO: decouple this file from the various options
        var kernelBuilder = Kernel.Builder.WithLogger(DefaultLogger<Kernel>.Instance);
        switch (config.Search.GetTextGeneratorConfig())
        {
            case AzureOpenAIConfig cfg:
                // Note: .WithAzureTextCompletionService() Not supported
                kernel = kernelBuilder.WithAzureChatCompletionService(
                    deploymentName: cfg.Deployment,
                    endpoint: cfg.Endpoint,
                    apiKey: cfg.APIKey).Build();
                break;

            case OpenAIConfig cfg:
                var textModels = new List<string>
                {
                    "text-ada-001",
                    "text-babbage-001",
                    "text-curie-001",
                    "text-davinci-001",
                    "text-davinci-002",
                    "text-davinci-003",
                };

                if (textModels.Contains(cfg.Model.ToLowerInvariant()))
                {
                    kernel = kernelBuilder.WithOpenAITextCompletionService(
                        modelId: cfg.Model, apiKey: cfg.APIKey, orgId: cfg.OrgId).Build();
                }
                else
                {
                    kernel = kernelBuilder.WithOpenAIChatCompletionService(
                        modelId: cfg.Model, apiKey: cfg.APIKey, orgId: cfg.OrgId).Build();
                }

                break;

            default:
                throw new SemanticMemoryException(
                    $"Unknown/unsupported text generator '{config.Search.TextGenerator.GetType().FullName}'");
        }

        services.AddTransient<SearchClient>(_ => new SearchClient(vectorDb, embeddingGenerator, kernel));
    }

    public static void UseContentStorage(this IServiceCollection services, SemanticMemoryConfig config)
    {
        // TODO: migrate to dynamic config
        const string AzureBlobs = "AZUREBLOBS";
        const string FileSystem = "FILESYSTEM";

        // TODO: decouple this file from the various storage options
        switch (config.ContentStorage.Type.ToUpperInvariant())
        {
            case AzureBlobs:
                services.UseAzureBlobStorage(config);
                break;

            case FileSystem:
                services.AddTransient<IContentStorage>(_ => new FileSystem(config.ContentStorage.FileSystem.Directory));
                break;

            default:
                throw new NotImplementedException($"Content storage type '{config.ContentStorage.Type}' not available");
        }
    }

    public static void UseOrchestrator(this IServiceCollection services, SemanticMemoryConfig config)
    {
        const string InProcess = "INPROCESS";
        const string Distributed = "DISTRIBUTED";

        services.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();

        switch (config.Orchestration.Type.ToUpperInvariant())
        {
            case InProcess:
                services.AddSingleton<IPipelineOrchestrator, InProcessPipelineOrchestrator>();
                break;

            case Distributed:
                services.AddSingleton<IPipelineOrchestrator, DistributedPipelineOrchestrator>();
                services.UseDistributedPipeline(config);
                break;

            default:
                throw new NotImplementedException($"Orchestration type '{config.Orchestration}' not available");
        }

        // Allow to instantiate this class directly
        services.AddSingleton<InProcessPipelineOrchestrator>();
    }

    public static void UseAzureBlobStorage(this IServiceCollection services, SemanticMemoryConfig config)
    {
        const string AzureIdentity = "AZUREIDENTITY";
        const string ConnectionString = "CONNECTIONSTRING";

        // Make configuration available
        services.AddSingleton(config.ContentStorage.AzureBlobs);

        switch (config.ContentStorage.AzureBlobs.Auth.ToUpperInvariant())
        {
            case AzureIdentity:
                services.AddSingleton<IContentStorage>(serviceProvider => new AzureBlob(
                    config.ContentStorage.AzureBlobs.Account,
                    config.ContentStorage.AzureBlobs.EndpointSuffix,
                    serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            case ConnectionString:
                services.AddSingleton<IContentStorage>(serviceProvider => new AzureBlob(
                    config.ContentStorage.AzureBlobs.ConnectionString,
                    config.ContentStorage.AzureBlobs.Container,
                    serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            default:
                throw new NotImplementedException($"Azure Blob auth type '{config.ContentStorage.AzureBlobs.Auth}' not available");
        }
    }

    public static void UseDistributedPipeline(this IServiceCollection services, SemanticMemoryConfig config)
    {
        const string AzureQueue = "AZUREQUEUE";
        const string RabbitMQ = "RABBITMQ";
        const string FileBasedQueue = "FILEBASEDQUEUE";

        // Factory for multiple queues
        services.AddSingleton<QueueClientFactory>(
            serviceProvider => new QueueClientFactory(() => serviceProvider.GetService<IQueue>()!));

        // Choose a Queue backend
        switch (config.Orchestration.DistributedPipeline.Type.ToUpperInvariant())
        {
            case AzureQueue:
                const string AzureIdentity = "AZUREIDENTITY";
                const string ConnectionString = "CONNECTIONSTRING";

                // Make configuration available
                services.AddSingleton(config.Orchestration.DistributedPipeline.AzureQueue);

                switch (config.Orchestration.DistributedPipeline.AzureQueue.Auth.ToUpperInvariant())
                {
                    case AzureIdentity:
                        services.AddTransient<IQueue>(serviceProvider => new AzureQueue(
                            config.Orchestration.DistributedPipeline.AzureQueue.Account,
                            config.Orchestration.DistributedPipeline.AzureQueue.EndpointSuffix,
                            serviceProvider.GetService<ILogger<AzureQueue>>()));
                        break;

                    case ConnectionString:
                        services.AddTransient<IQueue>(serviceProvider => new AzureQueue(
                            config.Orchestration.DistributedPipeline.AzureQueue.ConnectionString,
                            serviceProvider.GetService<ILogger<AzureQueue>>()));
                        break;

                    default:
                        throw new NotImplementedException($"Azure Queue auth type '{config.Orchestration.DistributedPipeline.AzureQueue.Auth}' not available");
                }

                break;

            case RabbitMQ:

                // Make configuration available
                services.AddSingleton(config.Orchestration.DistributedPipeline.RabbitMq);

                services.AddTransient<IQueue>(serviceProvider => new RabbitMqQueue(
                    config.Orchestration.DistributedPipeline.RabbitMq.Host,
                    config.Orchestration.DistributedPipeline.RabbitMq.Port,
                    config.Orchestration.DistributedPipeline.RabbitMq.Username,
                    config.Orchestration.DistributedPipeline.RabbitMq.Password,
                    serviceProvider.GetService<ILogger<RabbitMqQueue>>()!));
                break;

            case FileBasedQueue:

                // Make configuration available
                services.AddSingleton(config.Orchestration.DistributedPipeline.FileBasedQueue);

                services.AddTransient<IQueue>(serviceProvider => new FileBasedQueue(
                    config.Orchestration.DistributedPipeline.FileBasedQueue.Path,
                    serviceProvider.GetService<ILogger<FileBasedQueue>>()!));
                break;

            default:
                throw new NotImplementedException($"Queue type '{config.Orchestration.DistributedPipeline.Type}' not available");
        }
    }

    /// <summary>
    /// Register the handler as a DI service, passing the step name to ctor
    /// </summary>
    /// <param name="services">DI service collection</param>
    /// <param name="stepName">Pipeline step name</param>
    /// <typeparam name="THandler">Handler class</typeparam>
    public static void UseHandler<THandler>(this IServiceCollection services, string stepName) where THandler : class
    {
        services.AddTransient<THandler>(serviceProvider => ActivatorUtilities.CreateInstance<THandler>(serviceProvider, stepName));
    }
}
#pragma warning restore CA1724
