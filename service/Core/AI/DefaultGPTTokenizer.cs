// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI;

public class DefaultGPTTokenizer : ITextTokenizer
{
    private static readonly Tokenizer s_tokenizer = TiktokenTokenizer.CreateForModel(
        "gpt-4", new Dictionary<string, int> { { "<|im_start|>", 100264 }, { "<|im_end|>", 100265 } });

    public static int StaticCountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    public int CountTokens(string text)
    {
        return s_tokenizer.CountTokens(text);
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        return s_tokenizer.EncodeToTokens(text, out string? _).Select(t => t.Value).ToList();
    }
}
