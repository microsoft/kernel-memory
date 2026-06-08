// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Context;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory API extensions
/// </summary>
public static class KernelMemoryExtensions
{
    /// <summary>
    /// Search the given index for an answer to the given query
    /// and return it without streaming the content.
    /// </summary>
    /// <param name="memory">Memory instance</param>
    /// <param name="question">Question to answer</param>
    /// <param name="index">Optional index name</param>
    /// <param name="filter">Filter to match</param>
    /// <param name="filters">Filters to match (using inclusive OR logic). If 'filter' is provided too, the value is merged into this list.</param>
    /// <param name="minRelevance">Minimum Cosine Similarity required</param>
    /// <param name="options">Options for the request, such as whether to stream results</param>
    /// <param name="context">Unstructured data supporting custom business logic in the current request.</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Answer to the query, if possible</returns>
    public static async Task<MemoryAnswer> AskAsync(
        this IKernelMemory memory,
        string question,
        string? index = null,
        MemoryFilter? filter = null,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        SearchOptions? options = null,
        IContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var optionsOverride = options.Clone() ?? new SearchOptions();
        optionsOverride.Stream = false;

        return await memory.AskStreamingAsync(
                question: question,
                index: index,
                filter: filter,
                filters: filters,
                minRelevance: minRelevance,
                options: optionsOverride,
                context: context,
                cancellationToken)
            .FirstAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

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
            filters = [];
            if (filter == null) { filters.Add([]); }
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
