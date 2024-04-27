// Copyright (c) Microsoft. All rights reserved.

using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.TikToken;

/// <summary>
/// TikToken GPT2 tokenizer (gpt2.tiktoken)
/// </summary>
public class TikTokenGPT2Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("gpt2");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
