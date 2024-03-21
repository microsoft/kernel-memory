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
    /// Adds Mongodb as memory service, to store memory records.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasMemoryDb(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasConfig config)
    {
        builder.Services.AddMongoDbAtlasAsMemoryDb(config);
        return builder;
    }

    /// <summary>
    /// Adds Mongodb as content storage for files.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasStorage(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasConfig config)
    {
        builder.Services.AddMongoDbAtlasAsContentStorage(config);
        return builder;
    }

    /// <summary>
    /// Adds Mongodb as content storage service and memory service, for both files and memory records.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasMemoryDbAndStorage(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasConfig config)
    {
        builder.Services.AddMongoDbAtlasAsMemoryDbAndContentStorage(config);
        return builder;
    }
}

/// <summary>
/// setup Mongodb
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Adds MongoDbAtlasMemory as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddMongoDbAtlasAsMemoryDb(
        this IServiceCollection services,
        MongoDbAtlasConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbAtlasMemory>();
    }

    /// <summary>
    /// Adds MongoDbAtlasStorage as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddMongoDbAtlasAsContentStorage(
        this IServiceCollection services,
        MongoDbAtlasConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IContentStorage, MongoDbAtlasStorage>();
    }

    /// <summary>
    /// Adds MongoDbAtlasMemory and MongoDbAtlasStorage as services.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddMongoDbAtlasAsMemoryDbAndContentStorage(
        this IServiceCollection services,
        MongoDbAtlasConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbAtlasMemory>()
            .AddSingleton<IContentStorage, MongoDbAtlasStorage>();
    }
}
