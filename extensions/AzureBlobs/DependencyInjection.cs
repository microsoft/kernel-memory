// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.DocumentStorage.AzureBlobs;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureBlobsDocumentStorage(this IKernelMemoryBuilder builder, AzureBlobsConfig config)
    {
        builder.Services.AddAzureBlobsAsDocumentStorage(config);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobsAsDocumentStorage(this IServiceCollection services, AzureBlobsConfig config)
    {
        return services
            .AddSingleton<AzureBlobsConfig>(config)
            .AddSingleton<IDocumentStorage, AzureBlobsStorage>();
    }
}
