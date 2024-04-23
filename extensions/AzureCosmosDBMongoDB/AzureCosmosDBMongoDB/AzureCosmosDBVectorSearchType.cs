// Copyright (c) Microsoft. All rights reserved.


// ReSharper disable InconsistentNaming
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBVectorSearchType
{
    [JsonPropertyName("vector_ivf")]
    VectorIVF,

    [JsonProperty("vector_hnsw")]
    JsonPropertyName
}
