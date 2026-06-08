// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI;

namespace Microsoft.Chunkers.UnitTests.Helpers;

internal sealed class TwoCharsTestTokenizer : ITextTokenizer
{
    public int CountTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / 2d);
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        int length = text.Length;
        var tokens = new List<string>(length / 2 + length % 2);

        Span<char> buffer = stackalloc char[2];
        for (int i = 0; i < length; i += 2)
        {
            buffer[0] = text[i];
            if (i + 1 < length)
            {
                buffer[1] = text[i + 1];
                tokens.Add(new string(buffer));
            }
            else
            {
                tokens.Add(text[i].ToString());
            }
        }

        return tokens;
    }
}
