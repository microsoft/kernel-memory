// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory;
internal class SemanticKernelTextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextTokenizer _tokenizer;
    private readonly ITextEmbeddingGeneration _generation;
    private readonly SemanticKernelConfig _config;

    public int MaxTokens { get; }

    public SemanticKernelTextEmbeddingGenerator(ITextEmbeddingGeneration generation,
                                                SemanticKernelConfig config,
                                                ITextTokenizer tokenizer)
    {
        this._generation = generation ?? throw new ArgumentNullException(nameof(generation));
        this._tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer), "Tokenizer not specified. The token count might be incorrect, causing unexpected errors");

        this._tokenizer = tokenizer;
        this._config = config;
        this.MaxTokens = config.MaxTokenTotal;
    }

    /// <inheritdoc />
    public int CountTokens(string text) => this._tokenizer.CountTokens(text);

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        => this._generation.GenerateEmbeddingAsync(text, cancellationToken);
}
