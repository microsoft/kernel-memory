// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.ContentStorage.AzureBlobs;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.AI.AzureOpenAI;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureOpenAIEmbeddingGeneration(this IServiceCollection services, AzureOpenAIConfig config)
    {
        switch (config.Auth)
        {
            case "":
            case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
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

            case string y when y.Equals("APIKey", StringComparison.OrdinalIgnoreCase):
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

    public static IServiceCollection AddSemanticKernelWithAzureOpenAI(this IServiceCollection services, AzureOpenAIConfig config)
    {
        switch (config.Auth)
        {
            case "":
            case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
                return services
                    .AddSingleton<IKernel>(serviceProvider => Kernel.Builder
                        .WithLogger(serviceProvider.GetService<ILogger<Kernel>>() ?? DefaultLogger<Kernel>.Instance)
                        .WithAzureChatCompletionService(
                            deploymentName: config.Deployment,
                            endpoint: config.Endpoint,
                            credentials: new DefaultAzureCredential(),
                            alsoAsTextCompletion: true,
                            setAsDefault: true)
                        .Build());

            case string y when y.Equals("APIKey", StringComparison.OrdinalIgnoreCase):
                return services
                    .AddSingleton<IKernel>((serviceProvider) => Kernel.Builder
                        .WithLogger(serviceProvider.GetService<ILogger<Kernel>>() ?? DefaultLogger<Kernel>.Instance)
                        .WithAzureChatCompletionService(
                            deploymentName: config.Deployment,
                            endpoint: config.Endpoint,
                            apiKey: config.APIKey,
                            alsoAsTextCompletion: true,
                            setAsDefault: true)
                        .Build());

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
    }

    public static void AddAzureOpenAIEmbeddingGenerationToList(this ConfiguredServices<ITextEmbeddingGeneration> services, AzureOpenAIConfig config)
    {
        switch (config.Auth)
        {
            case "":
            case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
                services.Add(serviceProvider => new AzureTextEmbeddingGeneration(
                    modelId: config.Deployment,
                    endpoint: config.Endpoint,
                    credential: new DefaultAzureCredential(),
                    logger: serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            case string y when y.Equals("APIKey", StringComparison.OrdinalIgnoreCase):
                services.Add(serviceProvider => new AzureTextEmbeddingGeneration(
                    modelId: config.Deployment,
                    endpoint: config.Endpoint,
                    apiKey: config.APIKey,
                    logger: serviceProvider.GetService<ILogger<AzureBlob>>()));
                break;

            default:
                throw new NotImplementedException($"Azure OpenAI auth type '{config.Auth}' not available");
        }
    }
}
