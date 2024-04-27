// Copyright (c) Microsoft. All rights reserved.

using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.TikToken;

/// <summary>
/// TikToken GPT3 tokenizer (p50k_base.tiktoken)
/// </summary>
public class TikTokenGPT3Tokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel("text-davinci-003");

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
