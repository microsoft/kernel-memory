// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class SearchQuery
{
    [JsonPropertyName("index")]
    [JsonPropertyOrder(0)]
    public string? Index { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    [JsonPropertyOrder(1)]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("filters")]
    [JsonPropertyOrder(10)]
    public List<MemoryFilter> Filters { get; set; } = new();

    [JsonPropertyName("minRelevance")]
    [JsonPropertyOrder(2)]
    public double MinRelevance { get; set; } = 0;

    [JsonPropertyName("limit")]
    [JsonPropertyOrder(3)]
    public int Limit { get; set; } = -1;
}
