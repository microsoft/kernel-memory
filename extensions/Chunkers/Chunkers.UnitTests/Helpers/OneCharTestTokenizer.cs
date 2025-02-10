// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;

namespace Microsoft.Chunkers.UnitTests.Helpers;

internal sealed class OneCharTestTokenizer : ITextTokenizer
{
    public int CountTokens(string text)
    {
        return text.Length;
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        var tokens = new List<string>(text.Length);
        tokens.AddRange(text.Select(t => t.ToString()));
        return tokens;
    }
}
