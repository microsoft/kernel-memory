// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DocumentStorage;
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
    /// Adds Mongodb as document storage for files.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasStorage(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasConfig config)
    {
        builder.Services.AddMongoDbAtlasAsDocumentStorage(config);
        return builder;
    }

    /// <summary>
    /// Adds Mongodb as document storage and memory db, for both files and memory records.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithMongoDbAtlasMemoryDbAndDocumentStorage(
        this IKernelMemoryBuilder builder,
        MongoDbAtlasConfig config)
    {
        builder.Services.AddMongoDbAtlasAsMemoryDbAndDocumentStorage(config);
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
    public static IServiceCollection AddMongoDbAtlasAsDocumentStorage(
        this IServiceCollection services,
        MongoDbAtlasConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IDocumentStorage, MongoDbAtlasStorage>();
    }

    /// <summary>
    /// Adds MongoDbAtlasMemory and MongoDbAtlasStorage as services.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">Mongodb configuration.</param>
    public static IServiceCollection AddMongoDbAtlasAsMemoryDbAndDocumentStorage(
        this IServiceCollection services,
        MongoDbAtlasConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, MongoDbAtlasMemory>()
            .AddSingleton<IDocumentStorage, MongoDbAtlasStorage>();
    }
}
