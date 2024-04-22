// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBSimilarityTypes
{
    [JsonProperty("COS")]
    Cosine,

    [JsonProperty("IP")]
    InnerProduct,

    [JsonProperty("L2")]
    Eucledian
}
