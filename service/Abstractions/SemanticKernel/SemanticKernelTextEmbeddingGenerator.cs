// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory;
internal class SemanticKernelTextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly ITextEmbeddingGeneration _generation;
    private readonly SemanticKernelConfig _config;

    public int MaxTokens { get; }

    public SemanticKernelTextEmbeddingGenerator(ITextEmbeddingGeneration generation,
                                                SemanticKernelConfig config,
                                                ITextTokenizer tokenizer)
    {
        this._generation = generation;
        this._config = config;
        this._textTokenizer = tokenizer;

        this.MaxTokens = config.MaxTokenTotal;
    }

    /// <inheritdoc />
    public int CountTokens(string text) => this._textTokenizer.CountTokens(text);

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        => this._generation.GenerateEmbeddingAsync(text, cancellationToken);
}
