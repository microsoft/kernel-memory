// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Prompts;

namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithDefaultMimeTypeDetection(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IMimeTypeDetection, MimeTypesDetection>();
        return builder;
    }

    public static IKernelMemoryBuilder WithDefaultPromptProvider(
        this IKernelMemoryBuilder builder)
    {
        builder.AddSingleton<IPromptProvider, EmbeddedPromptProvider>();
        return builder;
    }
}
