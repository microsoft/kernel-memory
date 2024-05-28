// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.AI;

/// <summary>
/// Some (if not all) provider for embeddings can transform more than
/// one text chunk in a single call. This is used to limit the number
/// of API call making the process faster.
/// </summary>
public interface IBatchTextEmbeddingGenerator
{
    /// <summary>
    /// Generates the embeddings for a list of text chunks.
    /// </summary>
    /// <param name="text">The array of text chunks to transform.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Embedding[]> GenerateEmbeddingsAsync(
        string[] text,
        CancellationToken cancellationToken = default);
}
