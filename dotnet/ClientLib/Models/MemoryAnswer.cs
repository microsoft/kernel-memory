﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.Client.Models;

public class MemoryAnswer
{
    /// <summary>
    /// Client question.
    /// </summary>
    [JsonPropertyName("question")]
    [JsonPropertyOrder(1)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Content of the answer.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(2)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// List of the relevant sources used to produce the answer.
    /// Key = Document ID
    /// Value = List of partitions used from the document.
    /// </summary>
    [JsonPropertyName("relevantSources")]
    [JsonPropertyOrder(3)]
    public List<Citation> RelevantSources { get; set; } = new();

    public class Citation
    {
        /// <summary>
        /// Link to the source, if available.
        /// </summary>
        [JsonPropertyName("link")]
        [JsonPropertyOrder(1)]
        public string Link { get; set; } = string.Empty;

        /// <summary>
        /// Type of source, e.g. PDF, Word, Chat, etc.
        /// </summary>
        [JsonPropertyName("sourceContentType")]
        [JsonPropertyOrder(2)]
        public string SourceContentType { get; set; } = string.Empty;

        /// <summary>
        /// Name of the source, e.g. file name.
        /// </summary>
        [JsonPropertyName("sourceName")]
        [JsonPropertyOrder(3)]
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// List of chunks/blocks of text used.
        /// </summary>
        [JsonPropertyName("partitions")]
        [JsonPropertyOrder(4)]
        public List<Partition> Partitions { get; set; } = new();

        public class Partition
        {
            /// <summary>
            /// Content of the document partition, aka chunk/block of text.
            /// </summary>
            [JsonPropertyName("text")]
            [JsonPropertyOrder(1)]
            public string Text { get; set; } = string.Empty;

            /// <summary>
            /// Relevance of this partition against the given query.
            /// Value usually is between 0 and 1, when using cosine similarity.
            /// </summary>
            [JsonPropertyName("relevance")]
            [JsonPropertyOrder(2)]
            public float Relevance { get; set; } = 0;

            /// <summary>
            /// Timestamp about the file/text partition.
            /// </summary>
            [JsonPropertyName("lastUpdate")]
            [JsonPropertyOrder(4)]
            public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Serialize using .NET JSON serializer, e.g. to avoid ambiguity
    /// with other serializers and other options
    /// </summary>
    /// <param name="indented">Whether to keep the JSON readable, e.g. for debugging and views</param>
    /// <returns>JSON serialization</returns>
    public string ToJson(bool indented = false)
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = indented });
    }

    public MemoryAnswer FromJson(string json)
    {
        return JsonSerializer.Deserialize<MemoryAnswer>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new MemoryAnswer();
    }
}
