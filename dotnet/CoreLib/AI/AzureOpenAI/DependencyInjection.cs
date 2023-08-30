// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.AI;
using Microsoft.SemanticMemory.AI.AzureOpenAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithAzureOpenAITextCompletion(this MemoryClientBuilder builder, AzureOpenAIConfig config)
    {
        builder.Services.AddAzureOpenAITextGeneration(config);
        return builder;
    }

    public static MemoryClientBuilder WithAzureOpenAIEmbeddingGeneration(this MemoryClientBuilder builder, AzureOpenAIConfig config)
    {
        builder.Services.AddAzureOpenAIEmbeddingGeneration(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureOpenAIEmbeddingGeneration(this IServiceCollection services, AzureOpenAIConfig config)
    {
        switch (config.Auth)
        {
            case AzureOpenAIConfig.AuthTypes.AzureIdentity:
                return services
                    .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new AzureTextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        credential: new DefaultAzureCredential(),
                        loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

            case AzureOpenAIConfig.AuthTypes.APIKey:
                return services
                    .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new AzureTextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        apiKey: config.APIKey,
                        loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
    }

    public static IServiceCollection AddAzureOpenAITextGeneration(this IServiceCollection services, AzureOpenAIConfig config)
    {
        return services
            .AddSingleton<AzureOpenAIConfig>(config)
            .AddSingleton<ITextGeneration, AzureTextGeneration>();
    }
}
