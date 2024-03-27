// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory API extensions
/// </summary>
public static class KernelMemoryExtensions
{
    /// <summary>
    /// Return a list of synthetic memories of the specified type
    /// </summary>
    /// <param name="memory">Memory instance</param>
    /// <param name="syntheticType">Type of synthetic data to return</param>
    /// <param name="index">Optional name of the index where to search</param>
    /// <param name="filter">Filter to match</param>
    /// <param name="filters">Filters to match (using inclusive OR logic). If 'filter' is provided too, the value is merged into this list.</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of search results</returns>
    public static async Task<List<Citation>> SearchSyntheticsAsync(
        this IKernelMemory memory,
        string syntheticType,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (filters == null)
        {
            filters = new List<MemoryFilter>();
            if (filter == null) { filters.Add(new MemoryFilter()); }
        }

        if (filter != null)
        {
            filters.Add(filter);
        }

        foreach (var x in filters)
        {
            x.ByTag(Constants.ReservedSyntheticTypeTag, syntheticType);
        }

        SearchResult searchResult = await memory.SearchAsync(
            query: "",
            index: index,
            filters: filters,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return searchResult.Results;
    }

    /// <summary>
    /// Return a list of summaries matching the given filters
    /// </summary>
    /// <param name="memory">Memory instance</param>
    /// <param name="index">Optional name of the index where to search</param>
    /// <param name="filter">Filter to match</param>
    /// <param name="filters">Filters to match (using inclusive OR logic). If 'filter' is provided too, the value is merged into this list.</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of search results</returns>
    public static Task<List<Citation>> SearchSummariesAsync(
        this IKernelMemory memory,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        CancellationToken cancellationToken = default)
    {
        return SearchSyntheticsAsync(memory, Constants.TagsSyntheticSummary, index, filter, filters, cancellationToken);
    }
}
