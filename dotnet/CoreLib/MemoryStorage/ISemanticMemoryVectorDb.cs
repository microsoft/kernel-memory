// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public interface ISemanticMemoryVectorDb
{
    /// <summary>
    /// Create an index/collection
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="schema">Index/Collection schema</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task CreateIndexAsync(
        string indexName,
        VectorDbSchema schema,
        CancellationToken cancellationToken = default);

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
    /// <param name="withEmbeddings">Whether to include vector in the result</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>List of similar vectors, starting from the most similar</returns>
    IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(
        string indexName,
        Embedding<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a record
    /// </summary>
    /// <param name="indexName">Index/Collection name</param>
    /// <param name="record">Record to delete. Most Vector DB requires only the record ID to be set.</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns></returns>
    Task DeleteAsync(
        string indexName,
        MemoryRecord record,
        CancellationToken cancellationToken = default);

    // /// <summary>
    // /// Get list of records having a field matching a given value.
    // /// E.g. searching vectors by tag, for deletions.
    // /// </summary>
    // /// <param name="indexName">Index/Collection name</param>
    // /// <param name="fieldName">Field to search</param>
    // /// <param name="fieldIsCollection">Whether the field is a string or a collection of strings</param>
    // /// <param name="fieldValue">Value to match (if the field is a collection, the collection must contain the value)</param>
    // /// <param name="limit">Max number of records to return</param>
    // /// <param name="withEmbeddings">Whether to include vector in the result</param>
    // /// <param name="cancellationToken">Task cancellation token</param>
    // /// <returns>List of records</returns>
    // IAsyncEnumerable<MemoryRecord> SearchByFieldValueAsync(
    //     string indexName,
    //     string fieldName,
    //     bool fieldIsCollection,
    //     string fieldValue,
    //     int limit,
    //     bool withEmbeddings = false,
    //     CancellationToken cancellationToken = default);
}
