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
        SemanticMemoryConfig config = mgr.GetSection(SemanticMemoryConfig.PropertyName).Get<SemanticMemoryConfig>()!;

        // Copy the RAW handlers configuration from "SemanticMemory.Handlers". Binding is done later by each handler.
        // TODO: find a solution to move the binding logic here, simplifying the configuration classes.
        IConfigurationSection handlersConfigSection = mgr.GetSection(SemanticMemoryConfig.PropertyName).GetSection("Handlers");
        config.Handlers = new();
        foreach (IConfigurationSection bar in handlersConfigSection.GetChildren())
        {
            config.Handlers[bar.Key] = handlersConfigSection.GetSection(bar.Key);
        }

        services.AddSingleton(config);
        return config;
    }

    public static IServiceCollection UseSearchClient(this IServiceCollection services)
    {
        return
            services
                .UseContentStorage()
                .UseOrchestrator()
                .UseKernel();
    }

    public static IServiceCollection UseKernel(this IServiceCollection services)
    {
        services.AddTransient<SearchClient>(
            serviceProvider =>
            {
                ISemanticMemoryVectorDb vectorDb;
                ITextEmbeddingGeneration embeddingGenerator;
                IKernel kernel;

                var config = serviceProvider.GetRequiredService<SemanticMemoryConfig>();

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

                return new SearchClient(vectorDb, embeddingGenerator, kernel);
            });

        return services;
    }

    public static IServiceCollection UseContentStorage(this IServiceCollection services)
    {
        // TODO: migrate to dynamic config
        const string AzureBlobs = "AZUREBLOBS";
        const string FileSystem = "FILESYSTEM";

        services.AddSingleton(
            serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<SemanticMemoryConfig>();

                // TODO: decouple this file from the various storage options
                switch (config.ContentStorage.Type.ToUpperInvariant())
                {
                    case AzureBlobs:
                        return serviceProvider.UseAzureBlobStorage();

                    case FileSystem:
                        return new FileSystem(config.ContentStorage.FileSystem.Directory);

                    default:
                        throw new NotImplementedException($"Content storage type '{config.ContentStorage.Type}' not available");
                }
            });

        return services;
    }

    public static IServiceCollection UseOrchestrator(this IServiceCollection services)
    {
        const string InProcess = "INPROCESS";
        const string Distributed = "DISTRIBUTED";

        services.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();

        // Allow to instantiate this class directly
        services.AddSingleton<InProcessPipelineOrchestrator>();

        services.AddSingleton<IPipelineOrchestrator>(
            serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<SemanticMemoryConfig>();

                switch (config.Orchestration.Type.ToUpperInvariant())
                {
                    case InProcess:
                        return serviceProvider.GetRequiredService<InProcessPipelineOrchestrator>();

                    case Distributed:
                        return
                            new DistributedPipelineOrchestrator(
                                serviceProvider.GetRequiredService<IContentStorage>(),
                                serviceProvider.GetRequiredService<IMimeTypeDetection>(),
                                serviceProvider.GetQueueFactory(),
                                serviceProvider.GetRequiredService<ILogger<DistributedPipelineOrchestrator>>());

                    default:
                        throw new NotImplementedException($"Orchestration type '{config.Orchestration}' not available");
                }
            });

        return services;
    }

    private static IContentStorage UseAzureBlobStorage(this IServiceProvider serviceProvider)
    {
        const string AzureIdentity = "AZUREIDENTITY";
        const string ConnectionString = "CONNECTIONSTRING";

        var config = serviceProvider.GetRequiredService<SemanticMemoryConfig>();

        switch (config.ContentStorage.AzureBlobs.Auth.ToUpperInvariant())
        {
            case AzureIdentity:
                return
                    new AzureBlob(
                        config.ContentStorage.AzureBlobs.Account,
                        config.ContentStorage.AzureBlobs.EndpointSuffix,
                        serviceProvider.GetService<ILogger<AzureBlob>>());

            case ConnectionString:
                return
                    new AzureBlob(
                        config.ContentStorage.AzureBlobs.ConnectionString,
                        config.ContentStorage.AzureBlobs.Container,
                        serviceProvider.GetService<ILogger<AzureBlob>>());

            default:
                throw new NotImplementedException($"Azure Blob auth type '{config.ContentStorage.AzureBlobs.Auth}' not available");
        }
    }

    private static QueueClientFactory GetQueueFactory(this IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetRequiredService<SemanticMemoryConfig>();

        return new QueueClientFactory(() => CreateQueue());

        IQueue CreateQueue()
        {
            const string AzureQueue = "AZUREQUEUE";
            const string RabbitMQ = "RABBITMQ";
            const string FileBasedQueue = "FILEBASEDQUEUE";

            // Choose a Queue backend
            switch (config.Orchestration.DistributedPipeline.Type.ToUpperInvariant())
            {
                case AzureQueue:
                    const string AzureIdentity = "AZUREIDENTITY";
                    const string ConnectionString = "CONNECTIONSTRING";

                    switch (config.Orchestration.DistributedPipeline.AzureQueue.Auth.ToUpperInvariant())
                    {
                        case AzureIdentity:
                            return new AzureQueue(
                                config.Orchestration.DistributedPipeline.AzureQueue.Account,
                                config.Orchestration.DistributedPipeline.AzureQueue.EndpointSuffix,
                                serviceProvider.GetService<ILogger<AzureQueue>>());

                        case ConnectionString:
                            return new AzureQueue(
                                config.Orchestration.DistributedPipeline.AzureQueue.ConnectionString,
                                serviceProvider.GetService<ILogger<AzureQueue>>());

                        default:
                            throw new NotImplementedException($"Azure Queue auth type '{config.Orchestration.DistributedPipeline.AzureQueue.Auth}' not available");
                    }

                case RabbitMQ:

                    return new RabbitMqQueue(
                        config.Orchestration.DistributedPipeline.RabbitMq.Host,
                        config.Orchestration.DistributedPipeline.RabbitMq.Port,
                        config.Orchestration.DistributedPipeline.RabbitMq.Username,
                        config.Orchestration.DistributedPipeline.RabbitMq.Password,
                        serviceProvider.GetService<ILogger<RabbitMqQueue>>()!);

                case FileBasedQueue:

                    return new FileBasedQueue(
                        config.Orchestration.DistributedPipeline.FileBasedQueue.Path,
                        serviceProvider.GetService<ILogger<FileBasedQueue>>()!);

                default:
                    throw new NotImplementedException($"Queue type '{config.Orchestration.DistributedPipeline.Type}' not available");
            }
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
