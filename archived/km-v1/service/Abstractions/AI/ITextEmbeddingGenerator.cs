// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.AI;

public interface ITextEmbeddingGenerator : ITextTokenizer
{
    /// <summary>
    /// Max size of the LLM attention window, ie max tokens that can be processed.
    /// </summary>
    public int MaxTokens { get; }

    /// <summary>
    /// Generate the embedding vector for a given text
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Embedding vector</returns>
    public Task<Embedding> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
