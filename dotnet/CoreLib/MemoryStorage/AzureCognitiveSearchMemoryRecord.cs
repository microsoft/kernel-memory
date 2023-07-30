// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.Core20;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

// TODO: support bring your own index schema
internal sealed class AzureCognitiveSearchMemoryRecord
{
    private const string IdField = "id";
    internal const string VectorField = "embedding";
    private const string OwnerField = "owner";
    private const string SourceIdField = "source_id";
    private const string TagsField = "tags";
    private const string MetadataField = "metadata";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        MaxDepth = 10,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false
    };

    [JsonPropertyName(IdField)]
    public string Id { get; set; } = string.Empty;

#pragma warning disable CA1819
    [JsonPropertyName(VectorField)]
    public float[] Vector { get; set; } = Array.Empty<float>();
#pragma warning restore CA1819

    [JsonPropertyName(OwnerField)]
    public string Owner { get; set; } = string.Empty;

    [JsonPropertyName(SourceIdField)]
    public string SourceId { get; set; } = string.Empty;

    [JsonPropertyName(TagsField)]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName(MetadataField)]
    public string Metadata { get; set; } = string.Empty;

    public static VectorDbSchema GetSchema(int vectorSize)
    {
        return new VectorDbSchema
        {
            Fields = new List<VectorDbField>
            {
                new() { Name = IdField, Type = VectorDbField.FieldType.Text, IsKey = true, IsFilterable = true },
                new() { Name = VectorField, Type = VectorDbField.FieldType.Vector, VectorSize = vectorSize },
                new() { Name = OwnerField, Type = VectorDbField.FieldType.Text, IsFilterable = true },
                new() { Name = TagsField, Type = VectorDbField.FieldType.ListOfStrings, IsFilterable = true },
                new() { Name = SourceIdField, Type = VectorDbField.FieldType.Text, IsFilterable = true },
                new() { Name = MetadataField, Type = VectorDbField.FieldType.Text, IsFilterable = false },
            }
        };
    }

    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = DecodeId(this.Id),
            Owner = this.Owner,
            SourceId = this.SourceId,
            Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Metadata, s_jsonOptions) ?? new Dictionary<string, object>()
        };

        if (withEmbedding)
        {
            result.Vector = new Embedding<float>(this.Vector);
        }

        TagCollection tags = new();
        foreach (string[] keyValue in this.Tags.Select(tag => tag.Split('=', 2)))
        {
            tags.Add(keyValue[0], keyValue.Length == 1 ? null : keyValue[1]);
        }

        return result;
    }

    public static AzureCognitiveSearchMemoryRecord FromMemoryRecord(MemoryRecord record)
    {
        AzureCognitiveSearchMemoryRecord result = new()
        {
            Id = EncodeId(record.Id),
            Vector = record.Vector.Vector.ToArray(),
            Owner = record.Owner,
            SourceId = record.SourceId,
            Metadata = JsonSerializer.Serialize(record.Metadata, s_jsonOptions)
        };

        foreach (var tag in record.Tags.Pairs)
        {
            result.Tags.Add($"{tag.Key}={tag.Value}");
        }

        return result;
    }

    private static string EncodeId(string realId)
    {
        var bytes = Encoding.UTF8.GetBytes(realId);
        return Convert.ToBase64String(bytes);
    }

    private static string DecodeId(string encodedId)
    {
        var bytes = Convert.FromBase64String(encodedId);
        return Encoding.UTF8.GetString(bytes);
    }
}
