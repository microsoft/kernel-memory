// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core;
using KernelMemory.Core.Config.Embeddings;
using KernelMemory.Core.Embeddings;
using KernelMemory.Core.Embeddings.Cache;
using KernelMemory.Core.Embeddings.Providers;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Main.Services;

/// <summary>
/// Factory for creating embedding generators from configuration.
/// Supports caching decorator when cache is provided.
/// </summary>
public static class EmbeddingGeneratorFactory
{
    /// <summary>
    /// Creates an embedding generator from configuration.
    /// </summary>
    /// <param name="config">Embeddings configuration.</param>
    /// <param name="httpClient">HTTP client for API calls.</param>
    /// <param name="cache">Optional embedding cache (applies caching decorator if provided).</param>
    /// <param name="loggerFactory">Logger factory for creating component loggers.</param>
    /// <returns>The embedding generator instance.</returns>
    /// <exception cref="InvalidOperationException">If configuration type is not supported.</exception>
    public static IEmbeddingGenerator CreateGenerator(
        EmbeddingsConfig config,
        HttpClient httpClient,
        IEmbeddingCache? cache,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

        IEmbeddingGenerator innerGenerator = config switch
        {
            OllamaEmbeddingsConfig ollama => CreateOllamaGenerator(ollama, httpClient, loggerFactory),
            OpenAIEmbeddingsConfig openai => CreateOpenAIGenerator(openai, httpClient, loggerFactory),
            AzureOpenAIEmbeddingsConfig azure => CreateAzureOpenAIGenerator(azure, httpClient, loggerFactory),
            HuggingFaceEmbeddingsConfig hf => CreateHuggingFaceGenerator(hf, httpClient, loggerFactory),
            _ => throw new InvalidOperationException($"Unsupported embeddings config type: {config.GetType().Name}")
        };

        // Wrap with caching decorator if cache is provided
        if (cache != null)
        {
            var cacheLogger = loggerFactory.CreateLogger<CachedEmbeddingGenerator>();
            return new CachedEmbeddingGenerator(innerGenerator, cache, cacheLogger);
        }

        return innerGenerator;
    }

    /// <summary>
    /// Creates an Ollama embedding generator.
    /// </summary>
    private static IEmbeddingGenerator CreateOllamaGenerator(
        OllamaEmbeddingsConfig config,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<OllamaEmbeddingGenerator>();

        // Try to get known dimensions for the model
        var dimensions = Constants.EmbeddingDefaults.KnownModelDimensions.GetValueOrDefault(config.Model, defaultValue: 0);

        if (dimensions == 0)
        {
            // Unknown model - we'll validate on first use
            dimensions = Constants.EmbeddingDefaults.KnownModelDimensions.GetValueOrDefault(
                Constants.EmbeddingDefaults.DefaultOllamaModel, defaultValue: 1024);
        }

        return new OllamaEmbeddingGenerator(
            httpClient,
            config.BaseUrl,
            config.Model,
            dimensions,
            isNormalized: true, // Ollama models typically return normalized vectors
            logger);
    }

    /// <summary>
    /// Creates an OpenAI embedding generator.
    /// </summary>
    private static IEmbeddingGenerator CreateOpenAIGenerator(
        OpenAIEmbeddingsConfig config,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<OpenAIEmbeddingGenerator>();

        // Get known dimensions for the model
        var dimensions = Constants.EmbeddingDefaults.KnownModelDimensions.GetValueOrDefault(config.Model, defaultValue: 1536);

        return new OpenAIEmbeddingGenerator(
            httpClient,
            config.ApiKey,
            config.Model,
            dimensions,
            isNormalized: true, // OpenAI embeddings are typically normalized
            config.BaseUrl,
            logger);
    }

    /// <summary>
    /// Creates an Azure OpenAI embedding generator.
    /// Constructor signature: httpClient, endpoint, deployment, model, apiKey, vectorDimensions, isNormalized, logger
    /// </summary>
    private static IEmbeddingGenerator CreateAzureOpenAIGenerator(
        AzureOpenAIEmbeddingsConfig config,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<AzureOpenAIEmbeddingGenerator>();

        // Get known dimensions for the model
        var dimensions = Constants.EmbeddingDefaults.KnownModelDimensions.GetValueOrDefault(config.Model, defaultValue: 1536);

        return new AzureOpenAIEmbeddingGenerator(
            httpClient,
            config.Endpoint,
            config.Deployment,
            config.Model,
            config.ApiKey ?? string.Empty,
            dimensions,
            isNormalized: true, // Azure OpenAI embeddings are typically normalized
            logger);
    }

    /// <summary>
    /// Creates a HuggingFace embedding generator.
    /// </summary>
    private static IEmbeddingGenerator CreateHuggingFaceGenerator(
        HuggingFaceEmbeddingsConfig config,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<HuggingFaceEmbeddingGenerator>();

        // Get known dimensions for the model
        var dimensions = Constants.EmbeddingDefaults.KnownModelDimensions.GetValueOrDefault(config.Model, defaultValue: 384);

        return new HuggingFaceEmbeddingGenerator(
            httpClient,
            config.ApiKey ?? string.Empty,
            config.Model,
            dimensions,
            isNormalized: true, // Sentence-transformers models typically return normalized vectors
            config.BaseUrl,
            logger);
    }
}
