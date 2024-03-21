// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.KernelMemory.MemoryStorage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;

public sealed class AzureCosmosDBMongoVCoreMemoryRecord
{
    [BsonId]
    public string Id {get; set;} = string.Empty;
    [BsonElement("embedding")]
    public float[] Embedding {get; set;}
    [BsonElement("tags")]
    public List<string> Tags {get; set;} = new();
    [BsonElement("payload")]
    public string Payload { get; set; } = string.Empty;
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc, Representation = BsonType.DateTime)]
    public DateTime? Timestamp { get; set; }
    [BsonIgnoreIfDefault]
    public double similarityScore { get; set; }

    public static MemoryRecord ToMemoryRecord(BsonDocument document, bool withEmbedding = true)
    {
        var doc = document["document"].AsBsonDocument;
        MemoryRecord result = new()
        {
            Id = DecodeId(doc["_id"].AsString),
            Payload = BsonSerializer.Deserialize<Dictionary<string, object>>(doc["payload"].AsString)
                      ?? new Dictionary<string, object>()          
        };
        result.Payload.Add("timeStamp", doc["timestamp"]?.ToUniversalTime());

        if(withEmbedding)
        {
            result.Vector = doc["embedding"].AsBsonArray.Select(x => (float)x.AsDouble).ToArray();
        }

        foreach (string[] keyValue in doc["tags"].AsBsonArray.Select(tag => tag.AsString.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }

    public static AzureCosmosDBMongoVCoreMemoryRecord FromMemoryRecord (MemoryRecord record)
    {
        AzureCosmosDBMongoVCoreMemoryRecord result = new()
        {
            Id = EncodeId(record.Id),
            Embedding = record.Vector.Data.ToArray(),
            Payload = JsonSerializer.Serialize(record.Payload, s_jsonOptions),
            Timestamp = DateTime.UtcNow
        };

        foreach (var tag in record.Tags.Pairs)
        {
            result.Tags.Add($"{tag.Key}{Constants.ReservedEqualsChar}{tag.Value}");
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

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        AllowTrailingCommas = true,
        MaxDepth = 10,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false
    };
}