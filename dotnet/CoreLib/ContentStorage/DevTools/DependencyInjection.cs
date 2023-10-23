// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithSimpleFileStorage(this KernelMemoryBuilder builder, SimpleFileStorageConfig? config = null)
    {
        builder.Services.AddSimpleFileStorageAsContentStorage(config ?? new SimpleFileStorageConfig());
        return builder;
    }

    public static KernelMemoryBuilder WithSimpleFileStorage(this KernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleFileStorageAsContentStorage(directory);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleFileStorageAsContentStorage(this IServiceCollection services, SimpleFileStorageConfig config)
    {
        return services
            .AddSingleton<SimpleFileStorageConfig>(config)
            .AddSingleton<IContentStorage, SimpleFileStorage>();
    }

    public static IServiceCollection AddSimpleFileStorageAsContentStorage(this IServiceCollection services, string directory)
    {
        var config = new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleFileStorageAsContentStorage(config);
    }
}
