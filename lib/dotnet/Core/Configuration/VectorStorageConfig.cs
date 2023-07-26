// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;

public class VectorStorageConfig
{
    public class TypedItem
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StorageTypes Type { get; set; }
    }

    /// <summary>
    /// Supported vector storage types.
    /// TODO: add more types
    /// </summary>
    public enum StorageTypes
    {
        Unknown = 0,
        AzureCognitiveSearch = 1,
        // Qdrant = 2,
    }

    /// <summary>
    /// List of vector storage configurations to use. Normally just one
    /// but it's also possible to store embeddings on multiple services at the same time.
    /// </summary>
    public List<Dictionary<string, object>> VectorDbs { get; set; } = new();

    /// <summary>
    /// Deserialize and cast a configuration item to the proper configuration type
    /// </summary>
    /// <param name="position">Position in the <see cref="VectorDbs"/> property</param>
    /// <returns>Configuration object</returns>
    public object GetVectorDbConfig(int position)
    {
        if (this.VectorDbs.Count < position + 1)
        {
            throw new ConfigurationException($"List doesn't contain an element at position {position}");
        }

        var json = JsonSerializer.Serialize(this.VectorDbs[position]);
        var typedItem = JsonSerializer.Deserialize<TypedItem>(json);
        object? result;
        switch (typedItem?.Type)
        {
            case null:
                throw new ConfigurationException("Vector storage type is NULL");

            default:
                throw new ConfigurationException($"Vector storage type not supported: {typedItem.Type:G}");

            case StorageTypes.AzureCognitiveSearch:
                result = JsonSerializer.Deserialize<AzureCognitiveSearchConfig>(json);
                break;

                // case StorageTypes.Qdrant:
                //     result = JsonSerializer.Deserialize<QdrantConfig>(json);
                //     break;
        }

        if (result == null)
        {
            throw new ConfigurationException($"Unable to deserialize {typedItem.Type} configuration at position {position}");
        }

        return result;
    }
}
