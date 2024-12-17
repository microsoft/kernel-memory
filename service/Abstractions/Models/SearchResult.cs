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
    /// Whether the search didn't return any result
    /// </summary>
    [JsonPropertyName("noResult")]
    [JsonPropertyOrder(2)]
    public bool NoResult
    {
        get => this.Results == null || this.Results.Count == 0;
        private set { }
    }

    /// <summary>
    /// List of the relevant sources used to produce the answer.
    /// Key = Document ID
    /// Value = List of partitions used from the document.
    /// </summary>
    [JsonPropertyName("results")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Citation> Results { get; set; } = [];

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
