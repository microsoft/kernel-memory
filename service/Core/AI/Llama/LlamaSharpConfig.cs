// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.AI.Llama;

public class LlamaSharpConfig
{
    public string ModelPath { get; set; }
    public uint MaxTokenTotal { get; set; } = 4096;
    public int GpuLayerCount { get; set; } = 0;
    public uint Seed { get; set; } = 1337;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ModelPath))
        {
            throw new ArgumentOutOfRangeException(nameof(this.ModelPath), "The model path value is empty");
        }
    }
}
