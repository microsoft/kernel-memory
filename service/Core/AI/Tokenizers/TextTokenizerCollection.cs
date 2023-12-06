// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.KernelMemory.AI.Tokenizers;

public sealed class TextTokenizerCollection
{
    private static TextTokenizerCollection s_instance = new();

    private readonly ConcurrentDictionary<string, ITextTokenizer> _instances = new();

    private TextTokenizerCollection()
    {
    }

    public static TextTokenizerCollection Singleton()
    {
        return s_instance;
    }

    public TextTokenizerCollection Set(string name, ITextTokenizer instance)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name), "The tokenizer name is empty");
        }

        this._instances[name] = instance;
        return this;
    }

    public ITextTokenizer Get(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name), "The tokenizer name is empty");
        }

        if (this._instances.TryGetValue(name, out ITextTokenizer? instance))
        {
            return instance;
        }

        throw new KeyNotFoundException($"TextTokenizer '{name}' not defined");
    }
}
