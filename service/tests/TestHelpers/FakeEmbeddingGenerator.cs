// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace Microsoft.KM.TestHelpers;

public sealed class FakeEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly Dictionary<string, Embedding> _mocks = [];

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public void Mock(string input, float[] embedding)
    {
        this._mocks[input] = embedding;
    }

    public int CountTokens(string text) => 0;

    public IReadOnlyList<string> GetTokens(string text) => [];

    public int MaxTokens { get; } = 0;

    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (this._mocks.TryGetValue(text, out Embedding mock))
        {
            return Task.FromResult(mock);
        }

        throw new ArgumentOutOfRangeException(nameof(text), $"Test input '{text}' not supported");
    }
}
