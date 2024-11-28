// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.SQLServer;

/// <summary>
/// Configuration for the SQL Server memory store.
/// </summary>
public class SqlServerConfig
{
    /// <summary>
    /// The default SQL Server collections table name.
    /// </summary>
    internal const string DefaultMemoryCollectionTableName = "KMCollections";

    /// <summary>
    /// The default SQL Server memories table name.
    /// </summary>
    internal const string DefaultMemoryTableName = "KMMemories";

    /// <summary>
    /// The default SQL Server embeddings table name.
    /// </summary>
    internal const string DefaultEmbeddingsTableName = "KMEmbeddings";

    /// <summary>
    /// The default SQL Server tags table name.
    /// </summary>
    internal const string DefaultTagsTableName = "KMMemoriesTags";

    /// <summary>
    /// The default schema used by the SQL Server memory store.
    /// </summary>
    internal const string DefaultSchema = "dbo";

    /// <summary>
    /// The default vector size when using the native VECTOR type.
    /// </summary>
    internal const int DefaultVectorSize = 1536;

    /// <summary>
    /// The connection string to the SQL Server database.
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// The schema used by the SQL Server memory store.
    /// </summary>
    public string Schema { get; set; } = DefaultSchema;

    /// <summary>
    /// The SQL Server collections table name.
    /// </summary>
    public string MemoryCollectionTableName { get; set; } = DefaultMemoryCollectionTableName;

    /// <summary>
    /// The SQL Server memories table name.
    /// </summary>
    public string MemoryTableName { get; set; } = DefaultMemoryTableName;

    /// <summary>
    /// The SQL Server embeddings table name.
    /// </summary>
    public string EmbeddingsTableName { get; set; } = DefaultEmbeddingsTableName;

    /// <summary>
    /// The SQL Server tags table name.
    /// </summary>
    public string TagsTableName { get; set; } = DefaultTagsTableName;

    /// <summary>
    /// Whether to use native vector search or not.
    /// </summary>
    /// <remarks>
    /// Currently, the native Vector search is available on Azure SQL Database only.
    /// See <a href="https://learn.microsoft.com/sql/relational-databases/vectors/vectors-sql-server">Overview of vectors in the SQL Database Engine</a> for more information.
    /// </remarks>
    /// <seealso cref="VectorSize"/>
    public bool UseNativeVectorSearch { get; set; } = false;

    /// <summary>
    /// The vector size when using the native vector search.
    /// </summary>
    /// <remarks>
    /// Currently, the maximum supported vector size is 1998.
    /// See <a href="https://learn.microsoft.com/sql/relational-databases/vectors/vectors-sql-server">Overview of vectors in the SQL Database Engine</a> for more information.
    /// </remarks>
    /// <seealso cref="UseNativeVectorSearch"/>
    public int VectorSize { get; set; } = DefaultVectorSize;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.UseNativeVectorSearch)
        {
            if (this.VectorSize < 0)
            {
                throw new ConfigurationException("The vector size must be greater than 0");
            }

            if (this.VectorSize > 1998)
            {
                throw new ConfigurationException("The vector size must be less than or equal to 1998");
            }
        }
    }
}
