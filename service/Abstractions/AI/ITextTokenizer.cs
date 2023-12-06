// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI;

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
}
