// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

public enum AzureCosmosDBVectorSearchType
{
    [JsonProperty("vector_ivf")]
    VectorIVF,

    [JsonProperty("vector_hnsw")]
    VectorHNSW
}
