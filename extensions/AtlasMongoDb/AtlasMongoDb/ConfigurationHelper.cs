// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MemoryStorage;

namespace Alkampfer.KernelMemory.AtlasMongoDb;

public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Adds Mongodb as a storage service
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="configuration">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithAtlasMemoryDb(
        this IKernelMemoryBuilder builder,
        MongoDbKernelMemoryConfiguration configuration)
    {
        builder.Services.AddAtlasMongoDbAsStoreAndVectorDb(configuration);
        return builder;
    }
}

/// <summary>
/// setup Mongodb
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Adds MongoDbVectorMemory as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddAtlasMongoDbAsMemoryDb(
        this IServiceCollection services,
        MongoDbKernelMemoryConfiguration config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbVectorMemory>();
    }

    /// <summary>
    /// Adds MongoDbVectorMemory as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddAtlasMongoDbAsStoreAndVectorDb(
        this IServiceCollection services,
        MongoDbKernelMemoryConfiguration config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbVectorMemory>()
            .AddSingleton<IContentStorage, MongoDbKernelMemoryStorage>();
    }
}
