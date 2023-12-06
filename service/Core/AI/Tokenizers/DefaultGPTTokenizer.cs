// Copyright (c) Microsoft. All rights reserved.

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

    /// <summary>
    /// Internal static method to reduce instantiations when using the default tokenizer
    /// </summary>
    internal static int InternalCountTokens(string text)
    {
        return GPT3Tokenizer.Encode(text).Count;
    }
}
