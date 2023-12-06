// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class SearchResult
{
    private static readonly JsonSerializerOptions s_indentedJsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_notIndentedJsonOptions = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions s_caseInsensitiveJsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Client question.
    /// </summary>
    [JsonPropertyName("query")]
    [JsonPropertyOrder(1)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// List of the relevant sources used to produce the answer.
    /// Key = Document ID
    /// Value = List of partitions used from the document.
    /// </summary>
    [JsonPropertyName("results")]
    [JsonPropertyOrder(3)]
    public List<Citation> Results { get; set; } = new();

    /// <summary>
    /// Serialize using .NET JSON serializer, e.g. to avoid ambiguity
    /// with other serializers and other options
    /// </summary>
    /// <param name="indented">Whether to keep the JSON readable, e.g. for debugging and views</param>
    /// <returns>JSON serialization</returns>
    public string ToJson(bool indented = false)
    {
        return JsonSerializer.Serialize(this, indented ? s_indentedJsonOptions : s_notIndentedJsonOptions);
    }

    public MemoryAnswer FromJson(string json)
    {
        return JsonSerializer.Deserialize<MemoryAnswer>(json, s_caseInsensitiveJsonOptions)
               ?? new MemoryAnswer();
    }
}
