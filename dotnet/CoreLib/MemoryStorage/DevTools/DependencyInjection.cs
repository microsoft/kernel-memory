﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithSimpleVectorDb(this KernelMemoryBuilder builder, SimpleVectorDbConfig? config = null)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(config ?? new SimpleVectorDbConfig());
        return builder;
    }

    public static KernelMemoryBuilder WithSimpleVectorDb(this KernelMemoryBuilder builder, string directory)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(directory);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, SimpleVectorDbConfig? config = null)
    {
        return services
            .AddSingleton<SimpleVectorDbConfig>(config ?? new SimpleVectorDbConfig())
            .AddSingleton<IVectorDb, SimpleVectorDb>();
    }

    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, string directory)
    {
        var config = new SimpleVectorDbConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleVectorDbAsVectorDb(config);
    }
}
