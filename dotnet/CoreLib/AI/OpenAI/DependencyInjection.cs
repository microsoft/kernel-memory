// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

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

    public static IServiceCollection AddOpenAITextGeneration(this IServiceCollection services, OpenAIConfig config)
    {
        return services
            .AddSingleton<OpenAIConfig>(config)
            .AddSingleton<ITextGeneration, OpenAITextGeneration>()
            .AddSingleton<OpenAITextGeneration, OpenAITextGeneration>();
    }
}
