// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace redis_tests;

internal class MockEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly Dictionary<string, float[]> _embeddings = new();

    internal void AddFakeEmbedding(string str, float[] floats)
    {
        this._embeddings.Add(str, floats);
    }

    /// <inheritdoc />
    public int CountTokens(string text) => 0;

    /// <inheritdoc />
    public int MaxTokens => 0;

    /// <inheritdoc />
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(new Embedding(this._embeddings[text]));
}
