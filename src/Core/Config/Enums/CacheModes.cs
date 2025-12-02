// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Modes for embedding cache operations.
/// Controls whether the cache reads, writes, or both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheModes
{
    /// <summary>
    /// Both read from and write to cache (default).
    /// Cache hits return stored embeddings, misses are generated and stored.
    /// </summary>
    ReadWrite,

    /// <summary>
    /// Only read from cache, never write.
    /// Useful for read-only deployments or when cache is pre-populated.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Only write to cache, never read.
    /// Useful for warming up a cache without affecting current behavior.
    /// </summary>
    WriteOnly
}
