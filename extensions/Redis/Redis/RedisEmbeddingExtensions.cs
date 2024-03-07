// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Redis;

/// <summary>
/// Helper method for Embeddings.
/// </summary>
internal static class RedisEmbeddingExtensions
{
    public static byte[] VectorBlob(this Embedding embedding) => embedding.Data.ToArray().SelectMany(BitConverter.GetBytes).ToArray();
}
