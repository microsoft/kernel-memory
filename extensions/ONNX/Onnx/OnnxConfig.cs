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
    /// The maximum length of the response that the model will generate. See https://onnxruntime.ai/docs/genai/reference/config.html
    /// </summary>
    public uint MaxLength { get; set; } = 2048;

    /// <summary>
    /// The minimum length of the response that the model will generate. See https://onnxruntime.ai/docs/genai/reference/config.html
    /// </summary>
    public uint MinLength { get; set; } = 0;

    /// <summary>
    /// The option to use a share one buffer for past and present for efficiency. See https://onnxruntime.ai/docs/genai/reference/config.html
    /// </summary>
    public bool PastPresentShareBuffer { get; set; } = false;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate(bool allowIO = true)
    {
        if (string.IsNullOrWhiteSpace(this.ModelPath))
        {
            throw new ConfigurationException($"Onnx: {nameof(this.ModelPath)} is empty");
        }

        if (allowIO && !File.Exists(this.ModelPath))
        {
            throw new ConfigurationException($"Onnx: {nameof(this.ModelPath)} file not found");
        }
    }
}
