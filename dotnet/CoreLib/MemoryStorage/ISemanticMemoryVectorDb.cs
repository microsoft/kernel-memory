// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.MemoryStorage;

public interface ISemanticMemoryVectorDb
{
    /// <summary>
    /// Create an index/collection
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="vectorSize">Index/Collection vector size</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task CreateIndexAsync(
        string indexName,
        int vectorSize,
        CancellationToken cancellationToken = default);

    // TODO: revisit for custom schemas
    // /// <summary>
    // /// Create an index/collection
    // /// </summary>
    // /// <param name="indexName">Index/Collection name</param>
    // /// <param name="schema">Index/Collection schema</param>
    // /// <param name="cancellationToken">Task cancellation token</param>
    // Task CreateIndexAsync(
    //     string indexName,
    //     VectorDbSchema schema,
    //     CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an index/collection
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task DeleteIndexAsync(
        string indexName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert/Update a vector + payload
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="record">Vector + payload to save</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>Record ID</returns>
    Task<string> UpsertAsync(
        string indexName,
        MemoryRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of similar vectors (+payload)
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="embedding">Target vector to compare to</param>
    /// <param name="limit">Max number of results</param>
    /// <param name="minRelevanceScore">Minimum similarity required</param>
    /// <param name="filter">Values to match in the field used for tagging records (the field must be a list of strings)</param>
    /// <param name="withEmbeddings">Whether to include vector in the result</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>List of similar vectors, starting from the most similar</returns>
    IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string indexName,
        Embedding embedding,
        int limit,
        double minRelevanceScore = 0,
        MemoryFilter? filter = null,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of records having a field matching a given value.
    /// E.g. searching vectors by tag, for deletions.
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="filter">Values to match in the field used for tagging records (the field must be a list of strings)</param>
    /// <param name="limit">Max number of records to return</param>
    /// <param name="withEmbeddings">Whether to include vector in the result</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>List of records</returns>
    IAsyncEnumerable<MemoryRecord> GetListAsync(
        string indexName,
        MemoryFilter? filter = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a record
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="record">Record to delete. Most Vector DB requires only the record ID to be set.</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task DeleteAsync(
        string indexName,
        MemoryRecord record,
        CancellationToken cancellationToken = default);
}
