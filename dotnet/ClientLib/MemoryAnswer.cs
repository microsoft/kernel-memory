// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client;

public class MemoryAnswer
{
    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("RelevantSources")]
    public List<Dictionary<string, object>> RelevantSources { get; set; } = new();
}
