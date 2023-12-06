// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    private const string DefaultEmbeddingModel = "text-embedding-ada-002";
    private const int DefaultEmbeddingModelMaxToken = 8_191;
    private const string DefaultTextModel = "gpt-3.5-turbo-16k";
    private const int DefaultTextModelMaxToken = 16_384;

    public static IKernelMemoryBuilder WithOpenAIDefaults(
        this IKernelMemoryBuilder builder,
        string apiKey,
        string? organization = null,
        ITextTokenizer? textGenerationTokenizer = null,
        ITextTokenizer? textEmbeddingTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        bool onlyForRetrieval = false)
    {
        var openAIConfig = new OpenAIConfig
        {
            TextModel = DefaultTextModel,
            TextModelMaxTokenTotal = DefaultEmbeddingModelMaxToken,
            EmbeddingModel = DefaultEmbeddingModel,
            EmbeddingModelMaxTokenTotal = DefaultTextModelMaxToken,
            APIKey = apiKey,
            OrgId = organization
        };

        builder.Services.AddOpenAITextEmbeddingGeneration(openAIConfig, textEmbeddingTokenizer);
        builder.Services.AddOpenAITextGeneration(openAIConfig, textGenerationTokenizer);

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(new OpenAITextEmbeddingGenerator(
                config: openAIConfig,
                textTokenizer: textEmbeddingTokenizer,
                loggerFactory: loggerFactory));
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAI(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textGenerationTokenizer = null,
        ITextTokenizer? textEmbeddingTokenizer = null,
        bool onlyForRetrieval = false)
    {
        builder.WithOpenAITextEmbeddingGeneration(config, textEmbeddingTokenizer, onlyForRetrieval);
        builder.WithOpenAITextGeneration(config, textGenerationTokenizer);
        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAITextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        bool onlyForRetrieval = false)
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(config);
        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new OpenAITextEmbeddingGenerator(config, textTokenizer, loggerFactory: null));
        }

        return builder;
    }

    public static IKernelMemoryBuilder WithOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        builder.Services.AddOpenAITextGeneration(config, textTokenizer);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OpenAITextEmbeddingGenerator(
                    config: config,
                    textTokenizer: textTokenizer,
                    loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOpenAITextGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null)
    {
        return services
            .AddSingleton<ITextGenerator, OpenAITextGenerator>(serviceProvider => new OpenAITextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
