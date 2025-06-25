// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements

namespace Microsoft.KernelMemory;

public class ExtensionsAIConfig
{
    /// <summary>Gets or sets the maximum length of the response that the model will generate.</summary>
    public int MaxTokens { get; set; } = 8192;

    /// <summary>Gets or sets the name of the tokenizer used to count tokens.</summary>
    public string Tokenizer { get; set; } = "o200k";
}
