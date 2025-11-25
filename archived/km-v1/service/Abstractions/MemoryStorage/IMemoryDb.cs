// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.MemoryStorage;

/// <summary>
/// Common interface with all Memory API methods.
/// </summary>
public interface IMemoryDb
{
    /// <summary>
    /// Create an index/collection
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="vectorSize">Index/Collection vector size</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task CreateIndexAsync(
        string index,
        int vectorSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List indexes from the memory DB
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>List of indexes</returns>
    Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an index/collection
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task DeleteIndexAsync(
        string index,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert/Update a vector + payload.
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="record">Vector + payload to save</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>Record ID</returns>
    /// <exception cref="IndexNotFoundException">Error returned if the index where to write doesn't exist</exception>
    Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of similar vectors (+payload)
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="text">Text being searched</param>
    /// <param name="filters">Values to match in the field used for tagging records (the field must be a list of strings)</param>
    /// <param name="minRelevance">Minimum Cosine Similarity required</param>
    /// <param name="limit">Max number of results</param>
    /// <param name="withEmbeddings">Whether to include vector in the result</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>List of similar vectors, starting from the most similar</returns>
    IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of records having a field matching a given value.
    /// E.g. searching vectors by tag, for deletions.
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="filters">Values to match in the field used for tagging records (the field must be a list of strings)</param>
    /// <param name="limit">Max number of records to return</param>
    /// <param name="withEmbeddings">Whether to include vector in the result</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>List of records</returns>
    IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a memory record
    /// </summary>
    /// <param name="index">Index/Collection name</param>
    /// <param name="record">Record to delete. Most memory DBs require only the record ID to be set.</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task DeleteAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default);
}
