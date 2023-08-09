// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;

namespace Microsoft.SemanticMemory.Core.AI.AzureOpenAI;

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
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()))
                    .AddSingleton<AzureTextEmbeddingGeneration>(serviceProvider => new AzureTextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        credential: new DefaultAzureCredential(),
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()));

            case AzureOpenAIConfig.AuthTypes.APIKey:
                return services
                    .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new AzureTextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        apiKey: config.APIKey,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()))
                    .AddSingleton<AzureTextEmbeddingGeneration>(serviceProvider => new AzureTextEmbeddingGeneration(
                        modelId: config.Deployment,
                        endpoint: config.Endpoint,
                        apiKey: config.APIKey,
                        logger: serviceProvider.GetService<ILogger<AzureBlob>>()));

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
    }

    public static IServiceCollection AddAzureOpenAITextGeneration(this IServiceCollection services, AzureOpenAIConfig config)
    {
        return services
            .AddSingleton<AzureOpenAIConfig>(config)
            .AddSingleton<ITextGeneration, AzureTextGeneration>()
            .AddSingleton<AzureTextGeneration, AzureTextGeneration>();
    }
}
