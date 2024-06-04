// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.DocumentStorage.S3;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithS3DocumentStorage(this IKernelMemoryBuilder builder, S3Config config)
    {
        builder.Services.AddS3AsDocumentStorage(config);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddS3AsDocumentStorage(this IServiceCollection services, S3Config config)
    {
        return services
            .AddSingleton<S3Config>(config)
            .AddSingleton<IDocumentStorage, S3Storage>();
    }
}
