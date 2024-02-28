// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming
using Newtonsoft.Json;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBSimilarityType
{
    [JsonProperty("COS")]
    Cosine,
    [JsonProperty("IP")]
    InnerProduct,
    [JsonProperty("L2")]
    Eucledian
}
