// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;

// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithSimpleFileStorage(this IKernelMemoryBuilder builder, SimpleFileStorageConfig? config = null)
    {
        builder.Services.AddSimpleFileStorageAsDocumentStorage(config ?? new SimpleFileStorageConfig());
        return builder;
    }

    public static IKernelMemoryBuilder WithSimpleFileStorage(this IKernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleFileStorageAsDocumentStorage(directory);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleFileStorageAsDocumentStorage(this IServiceCollection services, SimpleFileStorageConfig config)
    {
        return services
            .AddSingleton<SimpleFileStorageConfig>(config)
            .AddSingleton<IDocumentStorage, SimpleFileStorage>();
    }

    public static IServiceCollection AddSimpleFileStorageAsDocumentStorage(this IServiceCollection services, string directory)
    {
        var config = new SimpleFileStorageConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleFileStorageAsDocumentStorage(config);
    }
}
