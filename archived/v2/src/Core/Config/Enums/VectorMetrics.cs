// Copyright (c) Microsoft. All rights reserved.
using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Distance/similarity metric for vector search
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VectorMetrics
{
    /// <summary>Cosine similarity (normalized dot product)</summary>
    Cosine,

    /// <summary>Euclidean distance (L2)</summary>
    L2,

    /// <summary>Inner product (dot product)</summary>
    InnerProduct,

    /// <summary>Manhattan distance (L1)</summary>
    L1
}
