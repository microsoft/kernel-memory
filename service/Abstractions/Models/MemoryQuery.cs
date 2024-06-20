// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class MemoryQuery
{
    [JsonPropertyName("index")]
    [JsonPropertyOrder(0)]
    public string? Index { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    [JsonPropertyOrder(1)]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("filters")]
    [JsonPropertyOrder(10)]
    public List<MemoryFilter> Filters { get; set; } = new();

    [JsonPropertyName("minRelevance")]
    [JsonPropertyOrder(2)]
    public double MinRelevance { get; set; } = 0;

    [JsonPropertyName("args")]
    [JsonPropertyOrder(100)]
    public Dictionary<string, object?> ContextArguments { get; set; } = new();
}
