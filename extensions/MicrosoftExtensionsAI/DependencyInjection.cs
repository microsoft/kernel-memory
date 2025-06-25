// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.ExtensionsAI;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Use an <see cref="IChatClient"/> as an <see cref="ITextGenerator"/> to generate text completions with this <see cref="IKernelMemoryBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="chatClient">The <see cref="IChatClient"/> to use for text generation.</param>
    /// <param name="config">Optional configuration for the instance.</param>
    /// <param name="tokenizer">Optional text tokenizer to use for token counting.</param>
    /// <returns>The builder provided as <paramref name="builder"/>.</returns>
    public static IKernelMemoryBuilder WithChatClient(
        this IKernelMemoryBuilder builder,
        IChatClient chatClient,
        ExtensionsAIConfig? config = null,
        ITextTokenizer? tokenizer = null)
    {
        ArgumentNullExceptionEx.ThrowIfNull(builder);
        ArgumentNullExceptionEx.ThrowIfNull(chatClient);

        builder.Services.AddSingleton<ITextGenerator, ExtensionsAITextGenerator>(serviceProvider =>
            new ExtensionsAITextGenerator(chatClient, config, tokenizer, serviceProvider.GetService<ILoggerFactory>()));

        return builder;
    }

    /// <summary>
    /// Use <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> to generate text embeddings.
    /// </summary>
    /// <param name="builder">Kernel Memory builder</param>
    /// <param name="embeddingGenerator">The <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> to use for embedding generation.</param>
    /// <param name="config">Optional configuration for the instance.</param>
    /// <param name="tokenizer">Optional text tokenizer to use for token counting.</param>
    /// <param name="onlyForRetrieval">Whether to use the <see cref="IEmbeddingGenerator"/> only for retrieval, not for ingestion.</param>
    /// <returns>The builder provided as <paramref name="builder"/>.</returns>
    public static IKernelMemoryBuilder WithEmbeddingGenerator(
        this IKernelMemoryBuilder builder,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ExtensionsAIConfig? config = null,
        ITextTokenizer? tokenizer = null,
        bool onlyForRetrieval = false)
    {
        ArgumentNullExceptionEx.ThrowIfNull(builder);
        ArgumentNullExceptionEx.ThrowIfNull(embeddingGenerator);

        builder.Services.AddSingleton<ITextEmbeddingGenerator, ExtensionsAIEmbeddingGenerator>(serviceProvider => new ExtensionsAIEmbeddingGenerator(
            embeddingGenerator, config, tokenizer, serviceProvider.GetService<ILoggerFactory>()));

        if (!onlyForRetrieval)
        {
            builder.AddIngestionEmbeddingGenerator(new ExtensionsAIEmbeddingGenerator(embeddingGenerator, config, tokenizer));
        }

        return builder;
    }
}
