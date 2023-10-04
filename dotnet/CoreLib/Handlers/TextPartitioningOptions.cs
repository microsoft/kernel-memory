// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Handlers;

/// <summary>
/// Represents options for text partitioning.
/// </summary>
public class TextPartitioningOptions
{
    /// <summary>
    /// The maximum number of tokens per line.
    /// </summary>
    public int MaxTokensPerLine { get; set; }

    /// <summary>
    /// The number of overlapping tokens.
    /// </summary>
    public int OverlappingTokens { get; set; }

    /// <summary>
    /// The maximum number of tokens per paragraph.
    /// </summary>
    public int MaxTokensPerParagraph { get; set; }
}
