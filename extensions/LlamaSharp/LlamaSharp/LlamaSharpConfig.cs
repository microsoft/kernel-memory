// Copyright (c) Microsoft. All rights reserved.

using System.IO;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class LlamaSharpConfig
{
    /// <summary>
    /// Path to the *.gguf file.
    /// </summary>
    public string ModelPath { get; set; } = "";

    /// <summary>
    /// Max number of tokens supported by the model.
    /// </summary>
    public uint MaxTokenTotal { get; set; } = 4096;

    /// <summary>
    /// Optional, number of GPU layers
    /// </summary>
    public int? GpuLayerCount { get; set; }

    public uint? Seed { get; set; } = 1337;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate(bool allowIO = true)
    {
        if (string.IsNullOrWhiteSpace(this.ModelPath))
        {
            throw new ConfigurationException($"LlamaSharp: {nameof(this.ModelPath)} is empty");
        }

        if (allowIO && !File.Exists(this.ModelPath))
        {
            throw new ConfigurationException($"LlamaSharp: {nameof(this.ModelPath)} file not found");
        }
    }
}
