// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client.Models;

public class MemoryQuery
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public MemoryFilter Filter { get; set; } = new();
}
