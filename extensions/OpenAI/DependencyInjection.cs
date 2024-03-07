// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    // Using GPT 3.5 Turbo - https://platform.openai.com/docs/models/gpt-3-5
    private const string DefaultTextModel = "gpt-3.5-turbo-16k";
    private const int DefaultTextModelMaxToken = 16_384;

    // Using Ada v2
    private const string DefaultEmbeddingModel = "text-embedding-ada-002";
    private const int DefaultEmbeddingModelMaxToken = 8_191;

    /// <summary>
    /// Use default OpenAI models (3.5-Turbo and Ada-002) and settings for ingestion and retrieval.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization ID (usually not required)</param>
    /// <param name="textGenerationTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <param name="textEmbeddingTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="loggerFactory">.NET Logger factory</param>
    /// <param name="onlyForRetrieval">Whether to use OpenAI defaults only for ingestion, and not for retrieval (search and ask API)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAIDefaults(
        this IKernelMemoryBuilder builder,
        string apiKey,
        string? organization = null,
        ITextTokenizer? textGenerationTokenizer = null,
        ITextTokenizer? textEmbeddingTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        bool onlyForRetrieval = false,
        HttpClient? httpClient = null)
    {
        textGenerationTokenizer ??= new DefaultGPTTokenizer();
        textEmbeddingTokenizer ??= new DefaultGPTTokenizer();

        var openAIConfig = new OpenAIConfig
        {
            TextModel = DefaultTextModel,
            TextModelMaxTokenTotal = DefaultTextModelMaxToken,
            EmbeddingModel = DefaultEmbeddingModel,
            EmbeddingModelMaxTokenTotal = DefaultEmbeddingModelMaxToken,
            APIKey = apiKey,
            OrgId = organization
        };
        openAIConfig.Validate();

        builder.Services.AddOpenAITextEmbeddingGeneration(openAIConfig, textEmbeddingTokenizer, httpClient);
        builder.Services.AddOpenAITextGeneration(openAIConfig, textGenerationTokenizer, httpClient);

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(new OpenAITextEmbeddingGenerator(
                config: openAIConfig,
                textTokenizer: textEmbeddingTokenizer,
                loggerFactory: loggerFactory,
                httpClient: httpClient));
        }

        return builder;
    }

    /// <summary>
    /// Use OpenAI models for ingestion and retrieval
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="textGenerationTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <param name="textEmbeddingTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="onlyForRetrieval">Whether to use OpenAI only for ingestion, not for retrieval (search and ask API)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAI(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textGenerationTokenizer = null,
        ITextTokenizer? textEmbeddingTokenizer = null,
        bool onlyForRetrieval = false,
        HttpClient? httpClient = null)
    {
        config.Validate();
        textGenerationTokenizer ??= new DefaultGPTTokenizer();
        textEmbeddingTokenizer ??= new DefaultGPTTokenizer();

        builder.WithOpenAITextEmbeddingGeneration(config, textEmbeddingTokenizer, onlyForRetrieval, httpClient);
        builder.WithOpenAITextGeneration(config, textGenerationTokenizer);
        return builder;
    }

    /// <summary>
    /// Use OpenAI to generate text embedding.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="onlyForRetrieval">Whether to use OpenAI only for ingestion, not for retrieval (search and ask API)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAITextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        bool onlyForRetrieval = false,
        HttpClient? httpClient = null)
    {
        config.Validate();
        textTokenizer ??= new DefaultGPTTokenizer();

        builder.Services.AddOpenAITextEmbeddingGeneration(config, httpClient: httpClient);
        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new OpenAITextEmbeddingGenerator(config, textTokenizer, loggerFactory: null, httpClient));
        }

        return builder;
    }

    /// <summary>
    /// Use OpenAI to generate text, e.g. answers and summaries.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        config.Validate();
        textTokenizer ??= new DefaultGPTTokenizer();

        builder.Services.AddOpenAITextGeneration(config, textTokenizer, httpClient);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        config.Validate();
        textTokenizer ??= new DefaultGPTTokenizer();

        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OpenAITextEmbeddingGenerator(
                    config: config,
                    textTokenizer: textTokenizer,
                    loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                    httpClient));
    }

    public static IServiceCollection AddOpenAITextGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        config.Validate();
        textTokenizer ??= new DefaultGPTTokenizer();

        return services
            .AddSingleton<ITextGenerator, OpenAITextGenerator>(serviceProvider => new OpenAITextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                httpClient));
    }
}
