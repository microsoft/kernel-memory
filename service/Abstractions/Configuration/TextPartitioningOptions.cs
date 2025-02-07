// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.Configuration;

/// <summary>
/// Represents options for text partitioning.
/// </summary>
public class TextPartitioningOptions
{
    /// <summary>
    /// The maximum number of tokens per paragraph.
    /// When partitioning a document, each partition usually contains one paragraph.
    /// </summary>
    public int MaxTokensPerParagraph { get; set; } = 1000;

    /// <summary>
    /// The number of overlapping tokens between chunks.
    /// </summary>
    public int OverlappingTokens { get; set; } = 100;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.MaxTokensPerParagraph < 1)
        {
            throw new ConfigurationException($"Text partitioning: {nameof(this.MaxTokensPerParagraph)} cannot be less than 1");
        }

        if (this.OverlappingTokens < 0)
        {
            throw new ConfigurationException($"Text partitioning: {nameof(this.OverlappingTokens)} cannot be less than 0");
        }

        if (this.OverlappingTokens >= this.MaxTokensPerParagraph)
        {
            throw new ConfigurationException($"Text partitioning: {nameof(this.OverlappingTokens)} must be less than {nameof(this.MaxTokensPerParagraph)}");
        }
    }
}
