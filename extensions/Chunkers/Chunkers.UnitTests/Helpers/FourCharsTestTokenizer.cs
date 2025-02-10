// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;

namespace Microsoft.Chunkers.UnitTests.Helpers;

internal sealed class FourCharsTestTokenizer : ITextTokenizer
{
    public int CountTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / 4d);
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        var tokens = new List<string>((text.Length + 3) / 4);

        Span<char> buffer = stackalloc char[4];
        for (int i = 0; i < text.Length; i += 4)
        {
            int tokenLength = Math.Min(4, text.Length - i);
            for (int j = 0; j < tokenLength; j++)
            {
                buffer[j] = text[i + j];
            }

            tokens.Add(new string(buffer.Slice(0, tokenLength)));
        }

        return tokens;
    }
}
