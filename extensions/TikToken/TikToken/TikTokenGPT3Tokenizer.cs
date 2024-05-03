// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.TikToken;

/// <summary>
/// TikToken GPT3 tokenizer (p50k_base.tiktoken)
/// </summary>
[Experimental("KMEXP01")]
public sealed class TikTokenGPT3Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("text-davinci-003");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
