// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Tokenizers;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// TikToken GPT2 tokenizer (gpt2.tiktoken)
/// </summary>
public sealed class GPT2Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = TiktokenTokenizer.CreateForModel("gpt2");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        return s_tokenizer.EncodeToTokens(text, out string? _).Select(t => t.Value).ToList();
    }
}
