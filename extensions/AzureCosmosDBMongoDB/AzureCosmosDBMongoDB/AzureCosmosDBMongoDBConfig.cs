// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.MemoryDb.AzureCosmosDBMongoDB;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Get more details about Azure Cosmos DB for MongoDB and these configs
/// at https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/vector-search
/// </summary>
public class AzureCosmosDBMongoDBConfig
{
    /// <summary>
    /// Connection string required to connect to Azure Cosmos DB for MongoDB
    /// see https://learn.microsoft.com/azure/cosmos-db/mongodb/vcore/quickstart-portal
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database name for the Mongo vCore DB
    /// </summary>
    public string DatabaseName { get; set; } = "default_KM_DB";

    /// <summary>
    /// Container name for the Mongo vCore DB
    /// </summary>
    public string CollectionName { get; set; } = "default_KM_Collection";

    /// <summary>
    /// Application name for the client for tracking and logging
    /// </summary>
    public string ApplicationName { get; set; } = "dotNet_Kernel_Memory";

    /// <summary>
    /// Index name for the Mongo vCore DB
    /// Index name for the MongoDB
    /// </summary>
    public string IndexName { get; set; } = "default_index";

    /// <summary>
    /// Kind: Type of vector index to create.
    ///     Possible options are:
    ///         - vector-ivf
    ///         - vector-hnsw: available as a preview feature only,
    ///                        to enable visit https://learn.microsoft.com/azure/azure-resource-manager/management/preview-features
    /// </summary>
    public AzureCosmosDBVectorSearchType Kind { get; set; } = AzureCosmosDBVectorSearchType.VectorHNSW;

    /// <summary>
    /// NumLists: This integer is the number of clusters that the inverted file (IVF) index uses to group the vector data.
    /// We recommend that numLists is set to documentCount/1000 for up to 1 million documents and to sqrt(documentCount)
    /// for more than 1 million documents. Using a numLists value of 1 is akin to performing brute-force search, which has
    /// limited performance.
    /// </summary>
    public int NumLists { get; set; } = 1;

    /// <summary>
    /// Similarity: Similarity metric to use with the IVF index.
    ///     Possible options are:
    ///         - COS (cosine distance),
    ///         - L2 (Euclidean distance), and
    ///         - IP (inner product).
    /// </summary>
    public AzureCosmosDBSimilarityTypes Similarity { get; set; } = AzureCosmosDBSimilarityTypes.Cosine;

    /// <summary>
    /// NumberOfConnections: The max number of connections per layer (16 by default, minimum value is 2, maximum value is
    /// 100). Higher m is suitable for datasets with high dimensionality and/or high accuracy requirements.
    /// </summary>
    public int NumberOfConnections { get; set; } = 16;

    /// <summary>
    /// EfConstruction: the size of the dynamic candidate list for constructing the graph (64 by default, minimum value is 4,
    /// maximum value is 1000). Higher ef_construction will result in better index quality and higher accuracy, but it will
    /// also increase the time required to build the index. EfConstruction has to be at least 2 * m
    /// </summary>
    public int EfConstruction { get; set; } = 64;

    /// <summary>
    /// EfSearch: The size of the dynamic candidate list for search (40 by default). A higher value provides better recall at
    /// the cost of speed.
    /// </summary>
    public int EfSearch { get; set; } = 40;
}
