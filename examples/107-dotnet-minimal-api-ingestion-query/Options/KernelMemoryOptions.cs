// Copyright (c) Microsoft. All rights reserved.

namespace Options;

public class KernelMemoryOptions
{
    public const string KernelMemory = "KernelMemory";
    public ServicesOptions Services { get; set; } = default!;
}
