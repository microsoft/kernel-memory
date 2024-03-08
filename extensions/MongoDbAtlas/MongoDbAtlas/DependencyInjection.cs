// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MongoDbAtlas;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Adds Mongodb as storage service and memory service, both storage and
    /// vectors are stored inside MongoDb.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="configuration">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasMemoryAndStorageDb(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasKernelMemoryConfiguration configuration)
    {
        builder.Services.AddMongoDbAtlasAsStoreAndMemoryDb(configuration);
        return builder;
    }

    /// <summary>
    /// Adds Mongodb only as memory service, storage db is configured separately so you
    /// can use local file system store or other storage.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="configuration">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasMemoryDb(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasKernelMemoryConfiguration configuration)
    {
        builder.Services.AddMongoDbAtlasAsMemoryDb(configuration);
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
    public static IServiceCollection AddMongoDbAtlasAsMemoryDb(
        this IServiceCollection services,
        MongoDbAtlasKernelMemoryConfiguration config)
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
    public static IServiceCollection AddMongoDbAtlasAsStoreAndMemoryDb(
        this IServiceCollection services,
        MongoDbAtlasKernelMemoryConfiguration config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbVectorMemory>()
            .AddSingleton<IContentStorage, MongoDbKernelMemoryStorage>();
    }
}
