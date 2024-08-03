// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace Microsoft.Redis.TestApplication;

internal sealed class MockEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly Dictionary<string, Embedding> _embeddings = new();

    internal void AddFakeEmbedding(string str, Embedding vector)
    {
        this._embeddings.Add(str, vector);
    }

    /// <inheritdoc />
    public int CountTokens(string text) => 0;

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text) => Array.Empty<string>();

    /// <inheritdoc />
    public int MaxTokens => 0;

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(this._embeddings[text]);
}
