// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.KernelMemory.Text;

namespace Microsoft.KernelMemory;

public class MemoryAnswer
{
    /// <summary>
    /// Used only when streaming. How to handle the current record.
    /// </summary>
    [JsonPropertyName("streamState")]
    [JsonPropertyOrder(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamStates? StreamState { get; set; } = null;

    /// <summary>
    /// Client question.
    /// </summary>
    [JsonPropertyName("question")]
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
    /// The token used by the model to generate the answer.
    /// </summary>
    /// <remarks>Not all the models and text generators return token usage information.</remarks>
    [JsonPropertyName("tokenUsage")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TokenUsage>? TokenUsage { get; set; }

    /// <summary>
    /// List of the relevant sources used to produce the answer.
    /// Key = Document ID
    /// Value = List of partitions used from the document.
    /// </summary>
    [JsonPropertyName("relevantSources")]
    [JsonPropertyOrder(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Citation> RelevantSources { get; set; } = [];

    /// <summary>
    /// Serialize using .NET JSON serializer, e.g. to avoid ambiguity
    /// with other serializers and other options
    /// </summary>
    /// <param name="optimizeForStream">Whether to reduce the payload size for SSE</param>
    /// <returns>JSON serialization</returns>
    public string ToJson(bool optimizeForStream)
    {
        if (!optimizeForStream || this.StreamState != StreamStates.Append)
        {
            return JsonSerializer.Serialize(this);
        }

        MemoryAnswer clone = JsonSerializer.Deserialize<MemoryAnswer>(JsonSerializer.Serialize(this))!;

#pragma warning disable CA1820
        if (clone.Question == string.Empty) { clone.Question = null!; }
#pragma warning restore CA1820

        if (clone.RelevantSources.Count == 0) { clone.RelevantSources = null!; }

        return JsonSerializer.Serialize(clone);
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLineNix(this.Result);

        if (!this.NoResult && this.RelevantSources is { Count: > 0 })
        {
            var sources = new Dictionary<string, string>();
            foreach (var x in this.RelevantSources)
            {
                string date = x.Partitions.First().LastUpdate.ToString("D", CultureInfo.CurrentCulture);
                sources[x.Index + x.Link] = $"  - {x.SourceName} [{date}]";
            }

            result.AppendLineNix("- Sources:");
            result.AppendLineNix(string.Join("\n", sources.Values));
        }

        return result.ToString();
    }
}
