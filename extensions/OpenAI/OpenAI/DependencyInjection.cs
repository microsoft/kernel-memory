// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.OpenAI;
using OpenAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    // Models at https://platform.openai.com/docs/models/gpt-4o-mini
    private const string DefaultTextModel = "gpt-4o-mini";
    private const int DefaultTextModelMaxToken = 32_768; // using 2 x max output tokens

    // Models at https://platform.openai.com/docs/guides/embeddings/embedding-models
    private const string DefaultEmbeddingModel = "text-embedding-ada-002";
    private const int DefaultEmbeddingModelMaxToken = 8_191;

    /// <summary>
    /// Use default OpenAI models and settings for ingestion and retrieval.
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
        builder.WithOpenAITextEmbeddingGeneration(config, textEmbeddingTokenizer, onlyForRetrieval, httpClient);
        builder.WithOpenAITextGeneration(config, textGenerationTokenizer, httpClient);
        return builder;
    }

    /// <summary>
    /// Use OpenAI models for ingestion and retrieval
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="openAIClient">Custom pre-configured OpenAI client</param>
    /// <param name="textGenerationTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <param name="textEmbeddingTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="onlyForRetrieval">Whether to use OpenAI only for ingestion, not for retrieval (search and ask API)</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAI(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textGenerationTokenizer = null,
        ITextTokenizer? textEmbeddingTokenizer = null,
        bool onlyForRetrieval = false)
    {
        config.Validate();
        builder.WithOpenAITextEmbeddingGeneration(config, openAIClient, textEmbeddingTokenizer, onlyForRetrieval);
        builder.WithOpenAITextGeneration(config, openAIClient, textGenerationTokenizer);
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
        builder.Services.AddOpenAITextEmbeddingGeneration(config, textTokenizer, httpClient: httpClient);
        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new OpenAITextEmbeddingGenerator(config, textTokenizer, loggerFactory: null, httpClient));
        }

        return builder;
    }

    /// <summary>
    /// Use OpenAI to generate text embedding.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="openAIClient">Custom pre-configured OpenAI client</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="onlyForRetrieval">Whether to use OpenAI only for ingestion, not for retrieval (search and ask API)</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAITextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null,
        bool onlyForRetrieval = false)
    {
        config.Validate();
        builder.Services.AddOpenAITextEmbeddingGeneration(config, openAIClient, textTokenizer);
        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new OpenAITextEmbeddingGenerator(config, openAIClient, textTokenizer));
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
        builder.Services.AddOpenAITextGeneration(config, textTokenizer, httpClient);
        return builder;
    }

    /// <summary>
    /// Use OpenAI to generate text, e.g. answers and summaries.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">OpenAI settings</param>
    /// <param name="openAIClient">Custom pre-configured OpenAI client</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        builder.Services.AddOpenAITextGeneration(config, openAIClient, textTokenizer);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OpenAITextEmbeddingGenerator(
                    config: config,
                    textTokenizer: textTokenizer,
                    loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                    httpClient));
    }

    public static IServiceCollection AddOpenAITextEmbeddingGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextEmbeddingGenerator>(
                serviceProvider => new OpenAITextEmbeddingGenerator(
                    config: config,
                    openAIClient: openAIClient,
                    textTokenizer: textTokenizer,
                    loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }

    public static IServiceCollection AddOpenAITextGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextGenerator, OpenAITextGenerator>(serviceProvider => new OpenAITextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                httpClient));
    }

    public static IServiceCollection AddOpenAITextGeneration(
        this IServiceCollection services,
        OpenAIConfig config,
        OpenAIClient openAIClient,
        ITextTokenizer? textTokenizer = null)
    {
        config.Validate();
        return services
            .AddSingleton<ITextGenerator, OpenAITextGenerator>(serviceProvider => new OpenAITextGenerator(
                config: config,
                openAIClient: openAIClient,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));
    }
}
