// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithContentDecoder<T>(
        this IKernelMemoryBuilder builder) where T : class, IContentDecoder
    {
        builder.Services.AddSingleton<IContentDecoder, T>();
        return builder;
    }

    public static IKernelMemoryBuilder WithContentDecoder(
        this IKernelMemoryBuilder builder, IContentDecoder decoder)
    {
        builder.Services.AddSingleton<IContentDecoder>(decoder);
        return builder;
    }
}
