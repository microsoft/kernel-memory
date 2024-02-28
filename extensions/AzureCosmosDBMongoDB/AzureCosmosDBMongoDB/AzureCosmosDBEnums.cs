// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public class AzureCosmosDBEnums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AzureCosmosDBSimilarityType Similarity { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AzureCosmosDBVectorSearchType SearchType { get; set; }
}
