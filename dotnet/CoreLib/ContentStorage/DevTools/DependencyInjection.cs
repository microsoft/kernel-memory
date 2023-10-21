// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.ContentStorage.DevTools;
using Microsoft.SemanticMemory.FileSystem.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithSimpleFileStorage(this MemoryClientBuilder builder, SimpleFileStorageConfig? config = null)
    {
        builder.Services.AddSimpleFileStorageAsContentStorage(config ?? new SimpleFileStorageConfig());
        return builder;
    }

    public static MemoryClientBuilder WithSimpleFileStorage(this MemoryClientBuilder builder, string directory)
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
