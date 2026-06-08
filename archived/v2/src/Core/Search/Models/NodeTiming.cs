// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Timing information for a single node.
/// Used to identify performance bottlenecks in multi-node searches.
/// </summary>
public sealed class NodeTiming
{
    /// <summary>
    /// Node ID.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// Time spent searching this node.
    /// Includes all indexes within the node.
    /// </summary>
    public required TimeSpan SearchTime { get; init; }
}
