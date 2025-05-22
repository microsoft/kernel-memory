// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.KernelMemory.MemoryStorage;
using Qdrant.Client.Grpc;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant.Client;

internal static class QdrantPointStruct
{
    private const string Id = "id";
    private const string Tags = "tags";
    private const string Payload = "payload";

    public static PointStruct FromMemoryRecord(MemoryRecord record)
    {
        return new PointStruct
        {
            Vectors = new Vectors { Vector = new Vector { Data = { record.Vector.Data.ToArray() } } },
            Payload =
            {
                [Id] = record.Id,
                [Tags] = record.Tags.Pairs.Select(tag => $"{tag.Key}{Constants.ReservedEqualsChar}{tag.Value}").ToArray(),
                [Payload] = Value.Parser.ParseJson(JsonSerializer.Serialize(record.Payload, QdrantConfig.JSONOptions)),
            }
        };
    }

    public static MemoryRecord ToMemoryRecord(ScoredPoint scoredPoint, bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = scoredPoint.Id.Uuid,
            Payload = scoredPoint.Payload.TryGetValue(Payload, out var payload)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonFormatter.Default.Format(payload.StructValue), QdrantConfig.JSONOptions) ?? []
                : []
        };

        if (withEmbedding)
        {
            result.Vector = new Embedding(scoredPoint.Vectors.Vector.Data.ToArray());
        }

        foreach (string[] keyValue in scoredPoint.Payload[Tags].ListValue.Values.Select(tag => tag.StringValue.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }

    public static MemoryRecord ToMemoryRecord(RetrievedPoint retrievedPoint, bool withEmbedding = true)
    {
        MemoryRecord result = new()
        {
            Id = retrievedPoint.Id.Uuid,
            Payload = retrievedPoint.Payload.TryGetValue(Payload, out var payload)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonFormatter.Default.Format(payload.StructValue), QdrantConfig.JSONOptions) ?? []
                : []
        };

        if (withEmbedding)
        {
            result.Vector = new Embedding(retrievedPoint.Vectors.Vector.Data.ToArray());
        }

        foreach (string[] keyValue in retrievedPoint.Payload[Tags].ListValue.Values.Select(tag => tag.StringValue.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }
}
