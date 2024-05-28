// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithSimpleVectorDb(this IKernelMemoryBuilder builder, SimpleVectorDbConfig? config = null)
    {
        builder.Services.AddSimpleVectorDbAsMemoryDb(config ?? new SimpleVectorDbConfig());
        return builder;
    }

    public static IKernelMemoryBuilder WithSimpleVectorDb(this IKernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleVectorDbAsMemoryDb(directory);
        return builder;
    }

    public static IKernelMemoryBuilder WithSimpleTextDb(this IKernelMemoryBuilder builder, SimpleTextDbConfig? config = null)
    {
        builder.Services.AddSimpleTextDbAsMemoryDb(config ?? new SimpleTextDbConfig());
        return builder;
    }

    public static IKernelMemoryBuilder WithSimpleTextDb(this IKernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleTextDbAsMemoryDb(directory);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleVectorDbAsMemoryDb(this IServiceCollection services, SimpleVectorDbConfig? config = null)
    {
        return services
            .AddSingleton<SimpleVectorDbConfig>(config ?? new SimpleVectorDbConfig())
            .AddSingleton<IMemoryDb, SimpleVectorDb>();
    }

    public static IServiceCollection AddSimpleVectorDbAsMemoryDb(this IServiceCollection services, string directory)
    {
        var config = new SimpleVectorDbConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleVectorDbAsMemoryDb(config);
    }

    public static IServiceCollection AddSimpleTextDbAsMemoryDb(this IServiceCollection services, SimpleTextDbConfig? config = null)
    {
        return services
            .AddSingleton<SimpleTextDbConfig>(config ?? new SimpleTextDbConfig())
            .AddSingleton<IMemoryDb, SimpleTextDb>();
    }

    public static IServiceCollection AddSimpleTextDbAsMemoryDb(this IServiceCollection services, string directory)
    {
        var config = new SimpleTextDbConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleTextDbAsMemoryDb(config);
    }
}
