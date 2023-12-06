// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;

namespace Microsoft.KernelMemory.AI.Tokenizers;

/// <summary>
/// Default tokenizer, using GPT3 logic.
/// </summary>
public class DefaultGPTTokenizer : ITextTokenizer
{
    /// <inheritdoc />
    public int CountTokens(string text)
    {
        return GPT3Tokenizer.Encode(text).Count;
    }

    /// <inheritdoc />
    public int CountTokens(StringBuilder? stringBuilder)
    {
        return GPT3Tokenizer.Encode(stringBuilder).Count;
    }
}
