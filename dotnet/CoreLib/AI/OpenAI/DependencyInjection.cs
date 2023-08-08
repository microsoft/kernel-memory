// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.AI.OpenAI;

public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(this IServiceCollection services, OpenAIConfig config)
    {
        return services
            .AddSingleton<OpenAITextEmbeddingGeneration>((serviceProvider) => new OpenAITextEmbeddingGeneration(
                modelId: config.EmbeddingModel,
                apiKey: config.APIKey,
                organization: config.OrgId,
                logger: serviceProvider.GetService<ILogger<OpenAITextEmbeddingGeneration>>()))
            .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new OpenAITextEmbeddingGeneration(
                modelId: config.EmbeddingModel,
                apiKey: config.APIKey,
                organization: config.OrgId,
                logger: serviceProvider.GetService<ILogger<OpenAITextEmbeddingGeneration>>()));
    }

    public static IServiceCollection AddSemanticKernelWithOpenAI(this IServiceCollection services, OpenAIConfig config)
    {
        var textModels = new List<string>
        {
            "text-ada-001",
            "text-babbage-001",
            "text-curie-001",
            "text-davinci-001",
            "text-davinci-002",
            "text-davinci-003",
        };

        if (textModels.Contains(config.TextModel.ToLowerInvariant()))
        {
            return services
                .AddSingleton<IKernel>((serviceProvider) => Kernel.Builder
                    .WithLogger(serviceProvider.GetService<ILogger<Kernel>>() ?? DefaultLogger<Kernel>.Instance)
                    .WithOpenAITextCompletionService(
                        modelId: config.TextModel,
                        apiKey: config.APIKey,
                        orgId: config.OrgId,
                        setAsDefault: true)
                    .Build());
        }

        return services
            .AddSingleton<IKernel>((serviceProvider) => Kernel.Builder
                .WithLogger(serviceProvider.GetService<ILogger<Kernel>>() ?? DefaultLogger<Kernel>.Instance)
                .WithOpenAIChatCompletionService(
                    modelId: config.TextModel,
                    apiKey: config.APIKey,
                    orgId: config.OrgId,
                    alsoAsTextCompletion: true,
                    setAsDefault: true)
                .Build());
    }
}
