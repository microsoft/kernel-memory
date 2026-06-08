// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class LlamaSharpConfig
{
    /// <summary>
    /// Settings for the model used for text generation. Chat models can be used too.
    /// </summary>
    public LlamaSharpModelConfig TextModel { get; set; } = new();

    /// <summary>
    /// Settings for the model used for text embedding generation.
    /// </summary>
    public LlamaSharpModelConfig EmbeddingModel { get; set; } = new();

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate(bool allowIO = true)
    {
        this.TextModel.Validate();
        this.EmbeddingModel.Validate();
    }
}
