// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class IndexCollection
{
    [JsonPropertyName("results")]
    [JsonPropertyOrder(1)]
    public List<IndexDetails> Results { get; set; } = new();
}
