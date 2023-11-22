// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.ContentStorage.AzureBlobs;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureBlobsStorage(this IKernelMemoryBuilder builder, AzureBlobsConfig config)
    {
        builder.Services.AddAzureBlobAsContentStorage(config);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureBlobAsContentStorage(this IServiceCollection services, AzureBlobsConfig config)
    {
        return services
            .AddSingleton<AzureBlobsConfig>(config)
            .AddSingleton<IContentStorage, AzureBlobsStorage>();
    }
}
