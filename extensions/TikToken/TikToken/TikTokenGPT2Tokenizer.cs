// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.TikToken;

/// <summary>
/// TikToken GPT2 tokenizer (gpt2.tiktoken)
/// </summary>
[Experimental("KMEXP01")]
public sealed class TikTokenGPT2Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("gpt2");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
