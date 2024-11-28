// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI;

public class TiktokenTokenizer : ITextTokenizer
{
    private readonly Tokenizer _tokenizer;

    public TiktokenTokenizer(string modelId)
    {
        try
        {
            this._tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel(modelId);
        }
        catch (NotSupportedException)
        {
            throw new KernelMemoryException("Autodetect failed");
        }
        catch (ArgumentNullException)
        {
            throw new KernelMemoryException("Autodetect failed");
        }
    }

    public int CountTokens(string text)
    {
        return this._tokenizer.CountTokens(text);
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._tokenizer.EncodeToTokens(text, out string? _).Select(t => t.Value).ToList();
    }
}
