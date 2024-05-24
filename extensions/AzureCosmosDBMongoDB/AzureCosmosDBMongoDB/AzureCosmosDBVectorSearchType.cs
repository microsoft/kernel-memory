// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable InconsistentNaming
namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

/// <summary>
/// Type of vector index to create. The options are vector-ivf and vector-hnsw.
/// </summary>
public enum AzureCosmosDBVectorSearchType
{
    /// <summary>
    /// vector-ivf is available on all cluster tiers
    /// </summary>
    [BsonElement("vector-ivf")]
    VectorIVF,

    /// <summary>
    /// vector-hnsw is available on M40 cluster tiers and higher.
    /// </summary>
    [BsonElement("vector-hnsw")]
    VectorHNSW
}

internal static class AzureCosmosDBVectorSearchTypeExtensions
{
    public static string GetCustomName(this AzureCosmosDBVectorSearchType type)
    {
        // Retrieve the FieldInfo object for the enum value, and check if it is null before accessing it.
        var fieldInfo = type.GetType().GetField(type.ToString());
        if (fieldInfo == null)
        {
            // Optionally handle the situation when the field is not found, such as logging a warning or throwing an exception.
            return type.ToString(); // Or handle differently as needed.
        }

        // Retrieve the BsonElementAttribute from the field, if present.
        var attribute = fieldInfo.GetCustomAttribute<BsonElementAttribute>();
        return attribute?.ElementName ?? type.ToString();
    }
}
