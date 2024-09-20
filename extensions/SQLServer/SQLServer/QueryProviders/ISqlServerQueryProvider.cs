// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

public interface ISqlServerQueryProvider
{
    /// <summary>
    /// Return SQL used to create a new index
    /// </summary>
    public string PrepareCreateIndexQuery(
        int sqlServerVersion,
        string index,
        int vectorSize);

    /// <summary>
    /// Return SQL used to get a list of indexes
    /// </summary>
    public string PrepareGetIndexesQuery();

    /// <summary>
    /// Return SQL used to delete an index
    /// </summary>
    public string PrepareDeleteIndexQuery(string index);

    /// <summary>
    /// Return SQL used to delete a memory record
    /// </summary>
    public string PrepareDeleteRecordQuery(string index);

    /// <summary>
    /// Return SQL used to get a list of memory records
    /// </summary>
    public string PrepareGetRecordsListQuery(
        string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    /// <summary>
    /// Return SQL used to get a list of similar memory records
    /// </summary>
    public string PrepareGetSimilarRecordsListQuery(
        string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    /// <summary>
    /// Return SQL used to upsert a batch of memory records
    /// </summary>
    public string PrepareUpsertRecordsBatchQuery(string index);

    /// <summary>
    /// Return SQL used to create all supporting tables
    /// </summary>
    public string PrepareCreateAllSupportingTablesQuery();
}
