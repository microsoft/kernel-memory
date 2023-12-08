// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

internal sealed class FakeEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly Dictionary<string, Embedding> _mocks = new();

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public void Mock(string input, float[] embedding)
    {
        this._mocks[input] = embedding;
    }

    public int CountTokens(string text)
    {
        return 0;
    }

    public int MaxTokens { get; } = 0;

    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (this._mocks.TryGetValue(text, out Embedding mock))
        {
            return Task.FromResult(mock);
        }

        throw new ArgumentOutOfRangeException($"Test input '{text}' not supported");
    }
}
