// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Context;

namespace Microsoft.KernelMemory.Search;

/// <summary>
/// Common interface for the search client used by Kernel Memory
/// </summary>
public interface ISearchClient
{
    /// <summary>
    /// Search for relevant memories, returning a list of partitions with details/citations.
    /// </summary>
    /// <param name="index">Index (aka collection) to search</param>
    /// <param name="query">Query used to search</param>
    /// <param name="filters">Additional filters</param>
    /// <param name="minRelevance">Minimum relevance of the results to return</param>
    /// <param name="limit">Max number of results to return</param>
    /// <param name="context">Optional context carrying optional information used by internal logic</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of relevant results matching the search criteria</returns>
    Task<SearchResult> SearchAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        IContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answer the given question, if possible, grounding the response with relevant memories matching the given criteria.
    /// </summary>
    /// <param name="index">Index (aka collection) to search for grounding information</param>
    /// <param name="question">Question to answer</param>
    /// <param name="filters">Filtering criteria to select memories to consider</param>
    /// <param name="minRelevance">Minimum relevance of the memories considered</param>
    /// <param name="context">Optional context carrying optional information used by internal logic</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Answer to the given question</returns>
    Task<MemoryAnswer> AskAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        IContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answer the given question, if possible, grounding the response with relevant memories matching the given criteria.
    /// </summary>
    /// <param name="index">Index (aka collection) to search for grounding information</param>
    /// <param name="question">Question to answer</param>
    /// <param name="filters">Filtering criteria to select memories to consider</param>
    /// <param name="minRelevance">Minimum relevance of the memories considered</param>
    /// <param name="context">Optional context carrying optional information used by internal logic</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Answer to the given question</returns>
    IAsyncEnumerable<MemoryAnswer> AskStreamingAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        IContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List the available memory indexes (aka collections).
    /// </summary>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of index names</returns>
    Task<IEnumerable<string>> ListIndexesAsync(CancellationToken cancellationToken = default);
}
