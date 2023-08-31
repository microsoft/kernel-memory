﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticMemory.MemoryStorage.AzureCognitiveSearch;

// TODO: support bring your own index schema
public sealed class AzureCognitiveSearchMemoryRecord
{
    internal const string IdField = "id";
    internal const string VectorField = "embedding";
    private const string TagsField = "tags";
    private const string PayloadField = "payload";

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
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding Vector { get; set; } = new();
#pragma warning restore CA1819

    [JsonPropertyName(TagsField)]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName(PayloadField)]
    public string Payload { get; set; } = string.Empty;

    public static VectorDbSchema GetSchema(int vectorSize)
    {
        return new VectorDbSchema
        {
            Fields = new List<VectorDbField>
            {
                new() { Name = IdField, Type = VectorDbField.FieldType.Text, IsKey = true },
                new() { Name = VectorField, Type = VectorDbField.FieldType.Vector, VectorSize = vectorSize },
                new() { Name = TagsField, Type = VectorDbField.FieldType.ListOfStrings, IsFilterable = true },
                new() { Name = PayloadField, Type = VectorDbField.FieldType.Text, IsFilterable = false },
            }
        };
    }

    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = DecodeId(this.Id),
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Payload, s_jsonOptions)
                      ?? new Dictionary<string, object>()
        };

        if (withEmbedding)
        {
            result.Vector = this.Vector;
        }

        foreach (string[] keyValue in this.Tags.Select(tag => tag.Split(Constants.ReservedEqualsSymbol, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }

    public static AzureCognitiveSearchMemoryRecord FromMemoryRecord(MemoryRecord record)
    {
        AzureCognitiveSearchMemoryRecord result = new()
        {
            Id = EncodeId(record.Id),
            Vector = record.Vector,
            Payload = JsonSerializer.Serialize(record.Payload, s_jsonOptions)
        };

        foreach (var tag in record.Tags.Pairs)
        {
            result.Tags.Add($"{tag.Key}{Constants.ReservedEqualsSymbol}{tag.Value}");
        }

        return result;
    }

    private static string EncodeId(string realId)
    {
        var bytes = Encoding.UTF8.GetBytes(realId);
        return Convert.ToBase64String(bytes).Replace('=', '_');
    }

    private static string DecodeId(string encodedId)
    {
        var bytes = Convert.FromBase64String(encodedId.Replace('_', '='));
        return Encoding.UTF8.GetString(bytes);
    }
}
