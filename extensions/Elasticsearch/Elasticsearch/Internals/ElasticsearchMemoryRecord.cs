// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

/// <summary>
/// Elasticsearch record.
/// </summary>
public sealed class ElasticsearchMemoryRecord
{
    internal const string IdField = "id";
    internal const string EmbeddingField = "embedding";

    /// <inheritdoc/>
    public const string TagsField = "tags";

    /// <inheritdoc/>
    internal static readonly string TagsName = TagsField + "." + nameof(ElasticsearchTag.Name).ToLowerInvariant();

    /// <inheritdoc/>
    internal static readonly string TagsValue = TagsField + "." + nameof(ElasticsearchTag.Value).ToLowerInvariant();

    private const string PayloadField = "payload";
    private const string ContentField = "content";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        MaxDepth = 10,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false
    };

    /// <summary>
    /// TBC
    /// </summary>
    [JsonPropertyName(IdField)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// TBC
    /// </summary>
    [JsonPropertyName(TagsField)]
    public List<ElasticsearchTag> Tags { get; set; } = [];

    /// <summary>
    /// TBC
    /// </summary>
    [JsonPropertyName(PayloadField)]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// TBC
    /// </summary>
    [JsonPropertyName(ContentField)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// TBC
    /// </summary>
    [JsonPropertyName(EmbeddingField)]
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding Vector { get; set; } = new();

    /// <summary>
    /// TBC
    /// </summary>
    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = this.Id,
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Payload, s_jsonOptions) ?? []
        };
        // TODO: remove magic string
        result.Payload["text"] = this.Content;

        if (withEmbedding)
        {
            result.Vector = this.Vector;
        }

        foreach (var tag in this.Tags)
        {
            result.Tags.Add(tag.Name, tag.Value);
        }

        return result;
    }

    /// <summary>
    /// TBC
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    public static ElasticsearchMemoryRecord FromMemoryRecord(MemoryRecord record)
    {
        ArgumentNullExceptionEx.ThrowIfNull(record, nameof(record), "The record is NULL");

        // TODO: remove magic strings
        string content = string.Empty;
        if (record.Payload.TryGetValue("text", out object? text))
        {
            content = text?.ToString() ?? string.Empty;
        }

        //string content = record.Payload["text"]?.ToString() ?? string.Empty;
        string documentId = string.Empty;
        if (record.Tags.TryGetValue("__document_id", out List<string?>? documentIdList))
        {
            documentId = documentIdList?[0] ?? string.Empty;
        }

        string filePart = string.Empty;
        if (record.Tags.TryGetValue("__file_part", out List<string?>? filePartList))
        {
            filePart = filePartList?[0] ?? string.Empty;
        }

        string betterId = $"{documentId}|{filePart}";

        record.Payload.Remove("text"); // We move the text to the content field. No need to index twice.

        ElasticsearchMemoryRecord result = new()
        {
            Id = record.Id,
            Vector = record.Vector,
            Payload = JsonSerializer.Serialize(record.Payload, s_jsonOptions),
            Content = content
        };

        foreach (var tag in record.Tags)
        {
            if (tag.Value == null || tag.Value.Count == 0)
            {
                // Key only, with no values
                result.Tags.Add(new ElasticsearchTag(name: tag.Key));
                continue;
            }

            foreach (var value in tag.Value)
            {
                // Key with one or more values
                result.Tags.Add(new ElasticsearchTag(name: tag.Key, value: value));
            }
        }

        return result;
    }
}
