// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.ContentStorage.AzureBlobs;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureBlobsStorage(this IKernelMemoryBuilder builder, AzureBlobsConfig config)
    {
        builder.Services.AddAzureBlobsAsContentStorage(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobsAsContentStorage(this IServiceCollection services, AzureBlobsConfig config)
    {
        return services
            .AddSingleton<AzureBlobsConfig>(config)
            .AddSingleton<IContentStorage, AzureBlobsStorage>();
    }
}
