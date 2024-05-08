// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

/// <summary>
/// Extensions methods for MemoryFilter.
/// </summary>
internal static class MemoryFilterExtensions
{
    /// <summary>
    /// Displays the MemoryFilter in a human-readable format.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static string ToDebugString(this MemoryFilter? filter)
    {
        if (filter == null)
        {
            return string.Empty;
        }

        // Prints all the tags in the record
        var tags = filter.Select(x => $"({x.Key}={string.Join("|", x.Value)})");
        return string.Join(" & ", tags);
    }

    /// <summary>
    /// Displays the MemoryFilter(s) in a human-readable format.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    public static string ToDebugString(this IEnumerable<MemoryFilter?>? filters)
    {
        if (filters == null)
        {
            return string.Empty;
        }

        // Prints all the tags in the record
        var tags = filters.Select(x => x.ToDebugString());
        return string.Join(" & ", tags);
    }
}
