// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class Citation
{
    private TagCollection _tags = new();

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

    /// <summary>
    /// List of document tags
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonPropertyOrder(5)]
    public TagCollection Tags
    {
        get { return this._tags; }
        set
        {
            this._tags = new();
            foreach (KeyValuePair<string, List<string?>> tag in value)
            {
                // Exclude internal tags
                if (tag.Key.StartsWith(Constants.ReservedTagsPrefix, StringComparison.OrdinalIgnoreCase)) { continue; }

                this._tags[tag.Key] = tag.Value;
            }
        }
    }

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
