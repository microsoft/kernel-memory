// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;

namespace Microsoft.KernelMemory.AI.Tokenizers;

public class DefaultGPTTokenizer : ITextTokenizer
{
    public int CountTokens(string text)
    {
        return this.Encode(text).Count;
    }

    public int CountTokens(StringBuilder? stringBuilder)
    {
        return this.Encode(stringBuilder).Count;
    }

    public int CountTokens(char[]? chars)
    {
        return this.Encode(chars).Count;
    }

    public int CountTokens(IEnumerable<char>? chars)
    {
        return this.Encode(chars).Count;
    }

    /// <inheritdoc />
    public List<int> Encode(string text)
    {
        return GPT3Tokenizer.Encode(text);
    }

    /// <inheritdoc />
    public List<int> Encode(StringBuilder? stringBuilder)
    {
        return GPT3Tokenizer.Encode(stringBuilder);
    }

    /// <inheritdoc />
    public List<int> Encode(char[]? chars)
    {
        return GPT3Tokenizer.Encode(chars);
    }

    /// <inheritdoc />
    public List<int> Encode(IEnumerable<char>? chars)
    {
        return GPT3Tokenizer.Encode(chars);
    }
}
