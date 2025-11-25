// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.AI;

/// <summary>
/// Interface to generate a list of embedding vectors, used by LLMs that support batch requests.
/// This is used to limit the number of API calls, making the process faster and reduce the risk of throttling.
/// </summary>
public interface ITextEmbeddingBatchGenerator
{
    /// <summary>
    /// The maximum number of elements that can be processed in a single batch.
    /// Some services and old models can process only one string at a time, others can process many more.
    /// The value depends on the service and the model used, and is configurable on each class.
    /// </summary>
    int MaxBatchSize { get; }

    /// <summary>
    /// Generates embeddings for a list of text chunks.
    /// </summary>
    /// <param name="textList">The list of text chunks to process.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Array of embedding vectors</returns>
    Task<Embedding[]> GenerateEmbeddingBatchAsync(
        IEnumerable<string> textList,
        CancellationToken cancellationToken = default);
}
