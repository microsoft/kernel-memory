// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class OnnxConfig
{
    /// <summary>
    /// Path to the directory containing the .ONNX file.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate(bool allowIO = true)
    {
        if (string.IsNullOrWhiteSpace(this.ModelPath))
        {
            throw new ConfigurationException($"Onnx: {nameof(this.ModelPath)} is empty");
        }

        if (allowIO && !Directory.Exists(this.ModelPath))
        {
            throw new ConfigurationException($"Onnx: Directory {Path.GetDirectoryName(this.ModelPath)} not found");
        }
    }
}
