// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.SemanticKernel;

/// <summary>
/// Semantic Kernel TextGenerator And TextEmbeddingGenerator Config
/// </summary>
public class SemanticKernelConfig
{
    /// <summary>
    /// Max size of the LLM attention window, ie max tokens that can be processed.
    /// </summary>
    public int MaxTokenTotal { get; set; } = 8191;
}
