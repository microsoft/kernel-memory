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
    public const string DefaultSchema = "dbo";

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
    /// Currently, Vector Search supports only Azure SQL Database and can handle vectors up to 1998 dimensions.
    /// See https://devblogs.microsoft.com/azure-sql/announcing-eap-native-vector-support-in-azure-sql-database for more information.
    /// </remarks>
    public bool UseNativeVectorSearch { get; set; } = false;
}
