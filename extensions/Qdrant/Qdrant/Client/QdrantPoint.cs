// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client;

/// <summary>
/// A record structure used by Qdrant that contains an embedding and metadata.
/// </summary>
internal class QdrantPoint<T> where T : DefaultQdrantPayload, new()
{
    [JsonPropertyName(QdrantConstants.PointIdField)]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName(QdrantConstants.PointVectorField)]
    [JsonConverter(typeof(Embedding.JsonConverter))]
    public Embedding Vector { get; set; } = new();

    [JsonPropertyName(QdrantConstants.PointPayloadField)]
    public T Payload { get; set; } = new();

    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = this.Payload.Id,
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Payload.Payload, QdrantConfig.JSONOptions)
                      ?? new Dictionary<string, object>()
        };

        if (withEmbedding)
        {
            result.Vector = this.Vector;
        }

        foreach (string[] keyValue in this.Payload.Tags.Select(tag => tag.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }

    public static QdrantPoint<T> FromMemoryRecord(MemoryRecord record)
    {
        return new QdrantPoint<T>
        {
            Vector = record.Vector,
            Payload = new T
            {
                Id = record.Id,
                Tags = record.Tags.Pairs.Select(tag => $"{tag.Key}{Constants.ReservedEqualsChar}{tag.Value}").ToList(),
                Payload = JsonSerializer.Serialize(record.Payload, QdrantConfig.JSONOptions),
            }
        };
    }
}
