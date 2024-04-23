// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBSimilarityTypes
{
    [JsonPropertyName("COS")]
    Cosine,

    [JsonPropertyName("IP")]
    InnerProduct,

    [JsonPropertyName("L2")]
    Eucledian
}
