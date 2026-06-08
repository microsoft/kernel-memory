// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Use Azure OpenAI to generate text embeddings.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">Azure OpenAI settings</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens sent to the embedding generator</param>
    /// <param name="loggerFactory">.NET Logger factory</param>
    /// <param name="onlyForRetrieval">Whether to use this embedding generator only during data ingestion, and not for retrieval (search and ask API)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithAzureOpenAITextEmbeddingGeneration(
        this IKernelMemoryBuilder builder,
        AzureOpenAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        bool onlyForRetrieval = false,
        HttpClient? httpClient = null)
    {
        builder.Services.AddAzureOpenAIEmbeddingGeneration(config, textTokenizer, httpClient);

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(
                new AzureOpenAITextEmbeddingGenerator(
                    config: config ?? new AzureOpenAIConfig(),
                    textTokenizer: textTokenizer,
                    loggerFactory: loggerFactory,
                    httpClient));
        }

        return builder;
    }

    /// <summary>
    /// Use Azure OpenAI to generate text, e.g. answers and summaries.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="config">Azure OpenAI settings</param>
    /// <param name="textTokenizer">Tokenizer used to count tokens used by prompts</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <returns>KM builder instance</returns>
    public static IKernelMemoryBuilder WithAzureOpenAITextGeneration(
        this IKernelMemoryBuilder builder,
        AzureOpenAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        builder.Services.AddAzureOpenAITextGeneration(config, textTokenizer, httpClient);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureOpenAIEmbeddingGeneration(
        this IServiceCollection services,
        AzureOpenAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        if (config == null && textTokenizer == null && httpClient == null)
        {
            return services.AddSingleton<ITextEmbeddingGenerator, AzureOpenAITextEmbeddingGenerator>();
        }

        config ??= new AzureOpenAIConfig();
        config.Validate();

        return services
            .AddSingleton<ITextEmbeddingGenerator>(serviceProvider => new AzureOpenAITextEmbeddingGenerator(
                config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                httpClient));
    }

    public static IServiceCollection AddAzureOpenAITextGeneration(
        this IServiceCollection services,
        AzureOpenAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        HttpClient? httpClient = null)
    {
        if (config == null && textTokenizer == null && httpClient == null)
        {
            return services.AddSingleton<ITextGenerator, AzureOpenAITextGenerator>();
        }

        config ??= new AzureOpenAIConfig();
        config.Validate();

        return services
            .AddSingleton<ITextGenerator>(serviceProvider => new AzureOpenAITextGenerator(
                config: config,
                textTokenizer: textTokenizer,
                loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
                httpClient: httpClient));
    }
}
