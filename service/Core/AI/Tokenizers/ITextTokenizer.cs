// Copyright (c) Microsoft. All rights reserved.

using System.Text;

namespace Microsoft.KernelMemory.AI.Tokenizers;

/// <summary>
/// Text tokenization interface.
/// </summary>
public interface ITextTokenizer
{
    /// <summary>
    /// Count the number of tokens contained in the given text.
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <returns>Number of tokens</returns>
    public int CountTokens(string text);

    /// <summary>
    /// Count the number of tokens contained in the given text.
    /// </summary>
    /// <param name="stringBuilder">Text to analyze</param>
    /// <returns>Number of tokens</returns>
    public int CountTokens(StringBuilder? stringBuilder);
}
