// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithOpenAIDefaults(this KernelMemoryBuilder builder, string apiKey, string? organization = null)
    {
        builder.Services.AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new OpenAITextEmbeddingGeneration(
            modelId: "text-embedding-ada-002",
            apiKey: apiKey,
            organization: organization,
            loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

        builder.Services.AddSingleton<ITextGeneration>(serviceProvider =>
            new OpenAITextGeneration(new OpenAIConfig
            {
                TextModel = "gpt-3.5-turbo-16k", EmbeddingModel = "text-embedding-ada-002", APIKey = apiKey, OrgId = organization
            }));

        return builder;
    }

    public static KernelMemoryBuilder WithOpenAI(this KernelMemoryBuilder builder, OpenAIConfig config)
    {
        builder.WithOpenAITextEmbedding(config);
        builder.WithOpenAITextGeneration(config);
        return builder;
    }

    public static KernelMemoryBuilder WithOpenAITextEmbedding(this KernelMemoryBuilder builder, OpenAIConfig config)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(config);
        return builder;
    }

    public static KernelMemoryBuilder WithOpenAITextGeneration(this KernelMemoryBuilder builder, OpenAIConfig config)
    {
        builder.Services.AddOpenAITextGeneration(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(this IServiceCollection services, OpenAIConfig config)
    {
        return services
            .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new OpenAITextEmbeddingGeneration(
                modelId: config.EmbeddingModel,
                apiKey: config.APIKey,
                organization: config.OrgId,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOpenAITextGeneration(this IServiceCollection services, OpenAIConfig config)
    {
        return services
            .AddSingleton<OpenAIConfig>(config)
            .AddSingleton<ITextGeneration, OpenAITextGeneration>();
    }
}
