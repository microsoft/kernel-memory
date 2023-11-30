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
    private const string DefaultEmbeddingModel = "text-embedding-ada-002";
    private const string DefaultTextModel = "gpt-3.5-turbo-16k";

    public static IKernelMemoryBuilder WithOpenAIDefaults(
        this IKernelMemoryBuilder builder,
        string apiKey,
        string? organization = null,
        bool onlyForRetrieval = false)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(new OpenAIConfig
        {
            TextModel = DefaultTextModel,
            EmbeddingModel = DefaultEmbeddingModel,
            APIKey = apiKey,
            OrgId = organization
        });

        builder.Services.AddOpenAITextGeneration(new OpenAIConfig
        {
            TextModel = DefaultTextModel,
            EmbeddingModel = DefaultEmbeddingModel,
            APIKey = apiKey,
            OrgId = organization
        });

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(new OpenAITextEmbeddingGeneration(
                modelId: DefaultEmbeddingModel,
                apiKey: apiKey,
                organization: organization));
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAI(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        bool onlyForRetrieval = false)
    {
        builder.WithOpenAITextEmbedding(config, onlyForRetrieval);
        builder.WithOpenAITextGeneration(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAITextEmbedding(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        bool onlyForRetrieval = false)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(config);
        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(new OpenAITextEmbeddingGeneration(
                modelId: config.EmbeddingModel,
                apiKey: config.APIKey,
                organization: config.OrgId));
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config)
    {
        builder.Services.AddOpenAITextGeneration(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        OpenAIConfig config)
    {
        return services
            .AddSingleton<ITextEmbeddingGeneration>(serviceProvider => new OpenAITextEmbeddingGeneration(
                modelId: config.EmbeddingModel,
                apiKey: config.APIKey,
                organization: config.OrgId,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOpenAITextGeneration(
        this IServiceCollection services,
        OpenAIConfig config)
    {
        return services
            .AddSingleton<ITextGeneration, OpenAITextGeneration>(serviceProvider => new OpenAITextGeneration(
                config: config,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
