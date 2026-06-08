// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.DocumentStorage.AWSS3;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAWSS3DocumentStorage(this IKernelMemoryBuilder builder, AWSS3Config config)
    {
        builder.Services.AddAWSS3AsDocumentStorage(config);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddAWSS3AsDocumentStorage(this IServiceCollection services, AWSS3Config config)
    {
        return services
            .AddSingleton<AWSS3Config>(config)
            .AddSingleton<IDocumentStorage, AWSS3Storage>();
    }
}
