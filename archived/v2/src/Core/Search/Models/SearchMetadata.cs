// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Metadata about search execution.
/// Includes telemetry, timing, and warnings (Q26).
/// </summary>
public sealed class SearchMetadata
{
    /// <summary>
    /// Number of nodes that completed successfully.
    /// </summary>
    public required int NodesSearched { get; init; }

    /// <summary>
    /// Number of nodes that were requested.
    /// NodesSearched may be less than NodesRequested if some nodes timed out or failed.
    /// </summary>
    public required int NodesRequested { get; init; }

    /// <summary>
    /// Total search execution time (end-to-end).
    /// </summary>
    public required TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Per-node timing information.
    /// Useful for identifying slow nodes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public NodeTiming[] NodeTimings { get; init; } = [];

    /// <summary>
    /// Warnings encountered during search.
    /// Examples: node timeouts, unavailable indexes, etc.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Warnings { get; init; } = [];
}
