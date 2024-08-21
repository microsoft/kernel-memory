// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.ML.Tokenizers;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory.AI.OpenAI;

public static class DefaultGPTTokenizer
{
    private static readonly Tokenizer s_tokenizer = Tokenizer.CreateTiktokenForModel(
        "gpt-4", new Dictionary<string, int> { { "<|im_start|>", 100264 }, { "<|im_end|>", 100265 } });

    public static int StaticCountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }
}
