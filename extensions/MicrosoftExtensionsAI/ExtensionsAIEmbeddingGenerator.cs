// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.AI.ExtensionsAI;

/// <summary>Provides an <see cref="ITextEmbeddingGenerator" /> that wraps an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
public sealed class ExtensionsAIEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ITextTokenizer _textTokenizer;
    private readonly ILogger<ExtensionsAIEmbeddingGenerator> _log;

    /// <inheritdoc/>
    public int MaxTokens { get; }

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

    /// <summary>Initializes a new instance of the <see cref="ExtensionsAIEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="config">Optional configuration for the instance.</param>
    /// <param name="textTokenizer">Optional text tokenizer to use for token counting.</param>
    /// <param name="loggerFactory">Optional logging factory to use for logging.</param>
    public ExtensionsAIEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ExtensionsAIConfig? config = null,
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullExceptionEx.ThrowIfNull(embeddingGenerator);

        config ??= new();

        this._embeddingGenerator = embeddingGenerator;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<ExtensionsAIEmbeddingGenerator>();
        this.MaxTokens = config.MaxTokens;
        this.MaxBatchSize = 1;
        this._textTokenizer = textTokenizer ?? TokenizerFactory.GetTokenizerForEncoding(string.IsNullOrEmpty(config.Tokenizer) ? "o200k" : config.Tokenizer)!;
    }

    /// <inheritdoc/>
    public int CountTokens(string text) => this._textTokenizer.CountTokens(text);

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text) => this._textTokenizer.GetTokens(text);

    /// <inheritdoc/>
    public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionEx.ThrowIfNull(text);
        var results = await this.GenerateEmbeddingBatchAsync([text], cancellationToken).ConfigureAwait(false);
        if (results.Length != 1)
        {
            throw new InvalidOperationException($"Expected exactly one embedding result, but received {results.Length}.");
        }

        return results[0];
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionEx.ThrowIfNull(textList);

        var results = await this._embeddingGenerator.GenerateAsync(textList, cancellationToken: cancellationToken).ConfigureAwait(false);
        return results.Select(embedding => new Embedding(embedding.Vector)).ToArray();
    }
}
