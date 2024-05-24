// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable InconsistentNaming
namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

/// <summary>
/// Similarity metric to use with the index. Possible options are COS (cosine distance), L2 (Euclidean distance), and IP (inner product).
/// </summary>
public enum AzureCosmosDBSimilarityTypes
{
    /// <summary>
    /// Cosine similarity
    /// </summary>
    [BsonElement("COS")]
    Cosine,

    /// <summary>
    /// Inner Product similarity
    /// </summary>
    [BsonElement("IP")]
    InnerProduct,

    /// <summary>
    /// Euclidean similarity
    /// </summary>
    [BsonElement("L2")]
    Euclidean
}

internal static class AzureCosmosDBSimilarityTypesExtensions
{
    public static string GetCustomName(this AzureCosmosDBSimilarityTypes type)
    {
        var attribute = type.GetType().GetField(type.ToString()).GetCustomAttribute<BsonElementAttribute>();
        return attribute?.ElementName ?? type.ToString();
    }
}
