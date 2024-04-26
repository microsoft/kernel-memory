// Copyright (c) Microsoft. All rights reserved.

using TiktokenSharp;

namespace Microsoft.KernelMemory.AI.Anthropic;

internal class TikTokenTokenizer : ITextTokenizer
{
    private static readonly TikToken s_tokenizer = TikToken.GetEncoding("cl100k_base");

    public int CountTokens(string text)
    {
        return s_tokenizer.Encode(text).Count;
    }
}
