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
    /// The maximum number of tokens per line, aka per sentence.
    /// When partitioning a block of text, the text will be split into sentences, that are then grouped into paragraphs.
    /// Note that this applies to any text format, including tables, code, chats, log files, etc.
    /// </summary>
    public int MaxTokensPerLine { get; set; } = 300;

    /// <summary>
    /// The number of overlapping tokens between paragraphs.
    /// </summary>
    public int OverlappingTokens { get; set; } = 100;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.MaxTokensPerParagraph < 1)
        {
            throw new ConfigurationException("The number of tokens per paragraph cannot be less than 1");
        }

        if (this.MaxTokensPerLine < 1)
        {
            throw new ConfigurationException("The number of tokens per line cannot be less than 1");
        }

        if (this.OverlappingTokens < 0)
        {
            throw new ConfigurationException("The number of overlapping tokens cannot be less than 0");
        }

        if (this.MaxTokensPerLine > this.MaxTokensPerParagraph)
        {
            throw new ConfigurationException("The number of tokens per line cannot be more than the tokens per paragraph");
        }

        if (this.OverlappingTokens >= this.MaxTokensPerParagraph)
        {
            throw new ConfigurationException("The number of overlapping tokens must be less than the tokens per paragraph");
        }
    }
}
