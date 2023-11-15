// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithAzureOpenAITextGeneration(this KernelMemoryBuilder builder, AzureOpenAIConfig config)
    {
        builder.Services.AddAzureOpenAITextGeneration(config);
        return builder;
    }

    public static KernelMemoryBuilder WithAzureOpenAIEmbeddingGeneration(this KernelMemoryBuilder builder, AzureOpenAIConfig config)
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
                    .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new AzureOpenAITextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        credential: new DefaultAzureCredential(),
                        loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

            case AzureOpenAIConfig.AuthTypes.APIKey:
                return services
                    .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new AzureOpenAITextEmbeddingGeneration(
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
