// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoVCore;

public sealed class AzureCosmosDBMongoVCoreMemoryRecord
{
    public String Id {get; set;} = string.Empty;
    public Embedding Vector {get; set;} = new();
    public List<String> Tags {get; set;} = new();
    public String Payload {get; set;} = string.Empty;

    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
    {
        MemoryRecord result = new();
        {
            Id = DecodeId(this.Id);
            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Payload, s_jsonOptions)
                      ?? new Dictionary<string, object>()
        };

        if(withEmbedding)
        {
            result.Vector = this.Vector;
        }

        foreach (string[] keyValue in this.Tags.Select(tag => tag.Split(Constants.ReservedEqualsChar, 2)))
        {
            string key = keyValue[0];
            string? value = keyValue.Length == 1 ? null : keyValue[1];
            result.Tags.Add(key, value);
        }

        return result;
    }

    public AzureCosmosDBMongoVCoreMemoryRecord FromMemoryRecord(MemoryRecord record)
    {
        AzureCosmosDBMongoVCoreMemoryRecord result = new()
        {
            Id = EncodeId(record.Id),
            Vector = record.Vector,
            Payload = JsonSerializer.Serialize(record.Payload, s_jsonOptions)
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


}