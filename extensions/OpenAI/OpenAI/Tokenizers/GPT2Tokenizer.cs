// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.Tokenizers;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// TikToken GPT2 tokenizer (gpt2.tiktoken)
/// </summary>
[Experimental("KMEXP01")]
public sealed class GPT2Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("gpt2");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        return s_tokenizer.Encode(text).Tokens;
    }
}
