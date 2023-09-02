﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

public class SearchQuery
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public MemoryFilter Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = -1;
}
