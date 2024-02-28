// Copyright (c) Microsoft. All rights reserved.

// ReSharper disable InconsistentNaming
using Newtonsoft.Json;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBVectorSearchType
{
    [JsonProperty("vector_ivf")]
    VectorIVF,

    [JsonProperty("vector_hnsw")]
    VectorHNSW
}
