// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using LLama;

namespace Microsoft.KernelMemory.AI.LlamaSharp;

public sealed class LLamaSharpTokenizer : ITextTokenizer
{
    private readonly LLamaContext _context;

    public LLamaSharpTokenizer(LLamaContext context)
    {
        this._context = context;
    }

    public int CountTokens(string text)
    {
        return this._context.Tokenize(text, special: true).Length;
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        StreamingTokenDecoder decoder = new(this._context);
        return this._context.Tokenize(text, special: true)
            .Select(x =>
            {
                decoder.Add(x);
                return decoder.Read();
            })
            .ToList();
    }
}
