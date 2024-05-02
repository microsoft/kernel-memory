// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.TikToken;

/// <summary>
/// GPT 3.5 and GPT 4+ tokenizer (cl100k_base.tiktoken + special tokens)
/// </summary>
[Experimental("KMEXP01")]
public sealed class TikTokenGPT4Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("gpt-4", new Dictionary<string, int> { { "<|im_start|>", 100264 }, { "<|im_end|>", 100265 } });

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
