// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client.Http;

internal sealed class ScrollVectorsResponse<T> : QdrantResponse where T : DefaultQdrantPayload, new()
{
    internal sealed class ScoredPoint : QdrantPoint<T>
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }

    internal sealed class ScrollResult
    {
        [JsonPropertyName("points")]
        public IEnumerable<QdrantPoint<T>> Points { get; set; } = [];
    }

    [JsonPropertyName("result")]
    public ScrollResult Results { get; set; } = new ScrollResult();
}
