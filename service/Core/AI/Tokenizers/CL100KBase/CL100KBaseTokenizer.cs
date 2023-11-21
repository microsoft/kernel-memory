// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.DeepDev;

namespace Microsoft.KernelMemory.AI.Tokenizers.CL100KBase;

public static class CL100KBaseTokenizer
{
    private static readonly ITokenizer s_tokenizer = InitializeTokenizer();

    public static List<int> Encode(string text)
    {
        return s_tokenizer.Encode(text, Array.Empty<string>());
    }

    private static ITokenizer InitializeTokenizer()
    {
        var assembly = typeof(CL100KBaseTokenizer).Assembly;
        var resourceName = $"{typeof(CL100KBaseTokenizer).Namespace}.cl100k_base.tiktoken";
        using var mergeableRanksStream = assembly.GetManifestResourceStream(resourceName)!;
        var regexPatternStr = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";
        var specialTokens = new Dictionary<string, int>()
        {
            { "<|endoftext|>", 100257},
            { "<|fim_prefix|>", 100258},
            { "<|fim_middle|>", 100259},
            { "<|fim_suffix|>", 100260},
            { "<|endofprompt|>", 100276}
        };

        return TokenizerBuilder.CreateTokenizer(mergeableRanksStream, specialTokens, regexPatternStr);
    }
}
