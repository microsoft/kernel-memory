// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;

internal sealed class FakeEmbeddingGenerator : ITextEmbeddingGeneration
{
    private readonly Dictionary<string, float[]> _mocks = new();

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data, CancellationToken cancellationToken = new())
    {
        return this.GenerateEmbeddingsAsync(data, null, cancellationToken);
    }

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data, Kernel? kernel = null, CancellationToken cancellationToken = new())
    {
        var result = new List<ReadOnlyMemory<float>>();
        foreach (var text in data)
        {
            if (this._mocks.TryGetValue(text, out float[]? mock))
            {
                result.Add(mock);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Test input '{text}' not supported");
            }
        }

        return Task.FromResult((IList<ReadOnlyMemory<float>>)result);
    }

    public void Mock(string input, float[] embedding)
    {
        this._mocks[input] = embedding;
    }
}
