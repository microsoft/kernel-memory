// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.AI.OpenAI;

public static class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithOpenAIDefaults(this MemoryClientBuilder builder, string apiKey, string? organization = null)
    {
        builder.Services.AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new OpenAITextEmbeddingGeneration(
            modelId: "text-embedding-ada-002",
            apiKey: apiKey,
            organization: organization,
            logger: serviceProvider.GetService<ILogger<OpenAITextEmbeddingGeneration>>()));

        builder.Services.AddSingleton<ITextGeneration>(serviceProvider =>
            new OpenAITextGeneration(new OpenAIConfig
            {
                TextModel = "gpt-3.5-turbo-16k", EmbeddingModel = "text-embedding-ada-002", APIKey = apiKey, OrgId = organization
            }));

        return builder;
    }

    public static MemoryClientBuilder WithOpenAI(this MemoryClientBuilder builder, OpenAIConfig config)
    {
        builder.WithOpenAITextEmbedding(config);
        builder.WithOpenAITextGeneration(config);
        return builder;
    }

    public static MemoryClientBuilder WithOpenAITextEmbedding(this MemoryClientBuilder builder, OpenAIConfig config)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(config);
        return builder;
    }

    public static MemoryClientBuilder WithOpenAITextGeneration(this MemoryClientBuilder builder, OpenAIConfig config)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(config);
        return builder;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(this IServiceCollection services, OpenAIConfig config)
    {
        return services
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
            .AddSingleton<ITextGeneration, OpenAITextGeneration>();
    }
}
