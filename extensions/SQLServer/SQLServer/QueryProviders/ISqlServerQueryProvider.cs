// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Microsoft.KernelMemory.MemoryDb.SQLServer.QueryProviders;

internal interface ISqlServerQueryProvider
{
    string GetCreateIndexQuery(int sqlServerVersion, string index, int vectorSize);

    string GetDeleteQuery(string index);

    string GetIndexDeleteQuery(string index);

    string GetIndexesQuery();

    string GetListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    string GetSimilarityListQuery(string index,
        ICollection<MemoryFilter>? filters,
        bool withEmbedding,
        SqlParameterCollection parameters);

    string GetUpsertBatchQuery(string index);

    string GetCreateTablesQuery();
}
