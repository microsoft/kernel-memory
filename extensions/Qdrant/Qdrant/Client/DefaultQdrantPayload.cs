// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client;

#pragma warning disable CA1852 // The class is inherited, it cannot be sealed

internal class DefaultQdrantPayload
{
    [JsonPropertyName(QdrantConstants.PayloadIdField)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName(QdrantConstants.PayloadTagsField)]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName(QdrantConstants.PayloadPayloadField)]
    public string Payload { get; set; } = string.Empty;
}
