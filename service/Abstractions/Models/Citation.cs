// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

public class Citation
{
    /// <summary>
    /// Link to the source, if available.
    /// </summary>
    [JsonPropertyName("link")]
    [JsonPropertyOrder(1)]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Link to the source, if available.
    /// </summary>
    [JsonPropertyName("index")]
    [JsonPropertyOrder(2)]
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// Link to the source, if available.
    /// </summary>
    [JsonPropertyName("documentId")]
    [JsonPropertyOrder(3)]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Link to the source, if available.
    /// </summary>
    [JsonPropertyName("fileId")]
    [JsonPropertyOrder(4)]
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Type of source, e.g. PDF, Word, Chat, etc.
    /// </summary>
    [JsonPropertyName("sourceContentType")]
    [JsonPropertyOrder(5)]
    public string SourceContentType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the source, e.g. file name.
    /// </summary>
    [JsonPropertyName("sourceName")]
    [JsonPropertyOrder(6)]
    public string SourceName { get; set; } = string.Empty;

#pragma warning disable CA1056
    /// <summary>
    /// URL of the source, used for web pages and external data
    /// </summary>
    [JsonPropertyName("sourceUrl")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceUrl { get; set; } = null;
#pragma warning restore CA1056

    /// <summary>
    /// List of chunks/blocks of text used.
    /// </summary>
    [JsonPropertyName("partitions")]
    [JsonPropertyOrder(8)]
    public List<Partition> Partitions { get; set; } = new();

    public class Partition
    {
        private TagCollection _tags = new();

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
        /// Partition number, zero based
        /// </summary>
        [JsonPropertyName("partitionNumber")]
        [JsonPropertyOrder(3)]
        public int PartitionNumber { get; set; } = 0;

        /// <summary>
        /// Text page number / Audio segment number / Video scene number
        /// </summary>
        [JsonPropertyName("sectionNumber")]
        [JsonPropertyOrder(4)]
        public int SectionNumber { get; set; } = 0;

        /// <summary>
        /// Timestamp about the file/text partition.
        /// </summary>
        [JsonPropertyName("lastUpdate")]
        [JsonPropertyOrder(10)]
        public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// List of document tags
        /// </summary>
        [JsonPropertyName("tags")]
        [JsonPropertyOrder(100)]
        public TagCollection Tags
        {
            get { return this._tags; }
            set
            {
                this._tags = new();
                foreach (KeyValuePair<string, List<string?>> tag in value)
                {
                    // Exclude internal tags
                    // if (tag.Key.StartsWith(Constants.ReservedTagsPrefix, StringComparison.OrdinalIgnoreCase)) { continue; }
                    this._tags[tag.Key] = tag.Value;
                }
            }
        }
    }
}
