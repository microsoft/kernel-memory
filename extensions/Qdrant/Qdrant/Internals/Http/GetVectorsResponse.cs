// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class GetVectorsResponse<T> : QdrantResponse where T : DefaultQdrantPayload, new()
{
    /// <summary>
    /// Array of vectors and their associated metadata
    /// </summary>
    [JsonPropertyName("result")]
    public IEnumerable<QdrantPoint<T>> Results { get; set; } = [];
}
