// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.AI.Embeddings;

internal sealed class FakeEmbeddingGenerator : ITextEmbeddingGeneration
{
    private readonly Dictionary<string, float[]> _mocks = new();

    public IReadOnlyDictionary<string, string> Attributes { get; } = new Dictionary<string, string>();

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data, CancellationToken cancellationToken = new())
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
