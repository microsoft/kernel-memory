// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.AI.OpenAI.GPT3;

namespace Microsoft.KernelMemory.AI.OpenAI;

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
    public static int StaticCountTokens(string text)
    {
        return GPT3Tokenizer.Encode(text).Count;
    }
}
