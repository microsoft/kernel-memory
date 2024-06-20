// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class MemoryAnswer
{
    private static readonly JsonSerializerOptions s_indentedJsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_notIndentedJsonOptions = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions s_caseInsensitiveJsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Client question.
    /// </summary>
    [JsonPropertyName("question")]
    [JsonPropertyOrder(1)]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("noResult")]
    [JsonPropertyOrder(2)]
    public bool NoResult { get; set; } = true;

    /// <summary>
    /// Content of the answer.
    /// </summary>
    [JsonPropertyName("noResultReason")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NoResultReason { get; set; }

    /// <summary>
    /// Content of the answer.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(10)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// List of the relevant sources used to produce the answer.
    /// Key = Document ID
    /// Value = List of partitions used from the document.
    /// </summary>
    [JsonPropertyName("relevantSources")]
    [JsonPropertyOrder(20)]
    public List<Citation> RelevantSources { get; set; } = new();

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

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine(this.Result);

        if (!this.NoResult)
        {
            var sources = new Dictionary<string, string>();
            foreach (var x in this.RelevantSources)
            {
                string date = x.Partitions.First().LastUpdate.ToString("D", CultureInfo.CurrentCulture);
                sources[x.Index + x.Link] = $"  - {x.SourceName} [{date}]";
            }

            result.AppendLine("- Sources:");
            result.AppendLine(string.Join("\n", sources.Values));
        }

        return result.ToString();
    }
}
