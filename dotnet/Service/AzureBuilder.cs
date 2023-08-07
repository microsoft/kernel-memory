// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticMemory.Core.AI.AzureOpenAI;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.Handlers;
using Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;
using Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;

namespace Microsoft.SemanticMemory.Service;

/// <summary>
/// Simple setup defaulting to Azure options
/// </summary>
public static class AzureBuilder
{
    private const string ConfigRoot = "SemanticMemory";

    public static WebApplicationBuilder CreateBuilder(out SemanticMemoryConfig config)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        config = builder.Configuration.GetSection(ConfigRoot).Get<SemanticMemoryConfig>()
                 ?? throw new ConfigurationException("Configuration is null");

        builder.Services.ConfigureRuntime(config);

        ConfigureIngestion(builder, config);
        ConfigureRetrieval(builder, config);

        return builder;
    }

    private static void ConfigureIngestion(WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        if (config.Service.RunHandlers)
        {
            // Register pipeline handlers as hosted services
            builder.Services.AddHandlerAsHostedService<TextExtractionHandler>("extract");
            builder.Services.AddHandlerAsHostedService<TextPartitioningHandler>("partition");
            builder.Services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>("gen_embeddings");
            builder.Services.AddHandlerAsHostedService<SaveEmbeddingsHandler>("save_embeddings");
        }

        // Orchestration Queues
        builder.Services.AddAzureQueue(builder.Configuration
            .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureQueue")
            .Get<AzureQueueConfig>()!);

        // File Storage
        builder.Services.AddAzureBlobAsContentStorage(builder.Configuration
            .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureBlobs")
            .Get<AzureBlobConfig>()!);
    }

    private static void ConfigureRetrieval(WebApplicationBuilder builder, SemanticMemoryConfig config)
    {
        // How to generate embeddings when searching for an answer
        builder.Services.AddAzureOpenAIEmbeddingGeneration(builder.Configuration
            .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureOpenAIEmbedding")
            .Get<AzureOpenAIConfig>()!);

        // Where to search embeddings when searching for an answer
        builder.Services.AddAzureCognitiveSearchAsVectorDb(builder.Configuration
            .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureCognitiveSearch")
            .Get<AzureCognitiveSearchConfig>()!);

        // How to generate an answer
        builder.Services.AddSemanticKernelWithAzureOpenAI(builder.Configuration
            .GetSection(ConfigRoot).GetSection("Services").GetSection("AzureOpenAIText")
            .Get<AzureOpenAIConfig>()!);
    }
}
