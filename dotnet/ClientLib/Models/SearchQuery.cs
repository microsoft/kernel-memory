// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client.Models;

public class SearchQuery
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public MemoryFilter Filter { get; set; } = new();
}
