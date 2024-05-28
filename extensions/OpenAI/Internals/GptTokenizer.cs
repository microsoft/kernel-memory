// Copyright (c) Microsoft. All rights reserved.
using Microsoft.ML.Tokenizers;

namespace Microsoft.KernelMemory.AI.OpenAI;

/// <summary>
/// This text tokenizer uses latest and optimized Tiktoken model directly
/// from Microsoft packages that offer optimized countTokens method.
/// </summary>
public class GptTokenizer : ITextTokenizer
{
    private readonly Tokenizer _tikToken;

    public GptTokenizer(string baseModelName)
    {
        this._tikToken = Tiktoken.CreateTiktokenForModel(baseModelName);
    }

    public int CountTokens(string text)
    {
        return this._tikToken.CountTokens(text);
    }
}
