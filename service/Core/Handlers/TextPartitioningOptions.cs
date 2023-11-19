// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Configuration;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Represents options for text partitioning.
/// </summary>
public class TextPartitioningOptions
{
    private readonly int _maxTokensPerParagraph = 1000;
    private readonly int _maxTokensPerLine = 300;
    private readonly int _overlappingTokens = 100;

    /// <summary>
    /// The maximum number of tokens per paragraph.
    /// </summary>
    public int MaxTokensPerParagraph
    {
        get
        {
            return this._maxTokensPerParagraph;
        }
        init
        {
            if (value < 1)
            {
                throw new ConfigurationException("The number of tokens per paragraph cannot be less than 1");
            }

            this._maxTokensPerParagraph = value;
        }
    }

    /// <summary>
    /// The maximum number of tokens per line.
    /// </summary>
    public int MaxTokensPerLine
    {
        get
        {
            return this._maxTokensPerLine;
        }
        init
        {
            if (value < 1)
            {
                throw new ConfigurationException("The number of tokens per line cannot be less than 1");
            }

            this._maxTokensPerLine = value;
        }
    }

    /// <summary>
    /// The number of overlapping tokens between paragraphs.
    /// </summary>
    public int OverlappingTokens
    {
        get
        {
            return this._overlappingTokens;
        }
        init
        {
            if (value < 0)
            {
                throw new ConfigurationException("The number of overlapping tokens cannot be less than 0");
            }

            this._overlappingTokens = value;
        }
    }
}
