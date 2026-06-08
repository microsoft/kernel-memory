// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory;

public sealed class KernelMemoryBuilderBuildOptions
{
    public readonly static KernelMemoryBuilderBuildOptions Default = new();

    public readonly static KernelMemoryBuilderBuildOptions WithVolatileAndPersistentData = new()
    {
        AllowMixingVolatileAndPersistentData = true
    };

    public bool AllowMixingVolatileAndPersistentData { get; set; } = false;
}
