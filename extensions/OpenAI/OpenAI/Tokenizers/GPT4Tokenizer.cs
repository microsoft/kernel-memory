// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Tokenizers;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// GPT 3.5 and GPT 4 tokenizer (cl100k_base.tiktoken + special tokens)
/// </summary>
public sealed class GPT4Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("gpt-4",
        new Dictionary<string, int> { { "<|im_start|>", 100264 }, { "<|im_end|>", 100265 } });

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTokens(string text)
    {
        return s_tokenizer.Encode(text, out string? _).Select(t => t.Value).ToList();
    }
}
