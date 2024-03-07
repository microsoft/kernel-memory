// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Postgres;

/// <summary>
/// Extensions for KernelMemoryBuilder
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add Postgres memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="config">Postgres configuration</param>
    public static IKernelMemoryBuilder WithPostgresMemoryDb(this IKernelMemoryBuilder builder, PostgresConfig config)
    {
        builder.Services.AddPostgresAsMemoryDb(config);
        return builder;
    }

    /// <summary>
    /// Kernel Memory Builder extension method to add Postgres memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="connString">Postgres connection string</param>
    public static IKernelMemoryBuilder WithPostgresMemoryDb(this IKernelMemoryBuilder builder, string connString)
    {
        builder.Services.AddPostgresAsMemoryDb(connString);
        return builder;
    }
}

/// <summary>
/// Extensions for KernelMemoryBuilder and generic DI
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject Postgres as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Postgres configuration</param>
    public static IServiceCollection AddPostgresAsMemoryDb(this IServiceCollection services, PostgresConfig config)
    {
        return services
            .AddSingleton<PostgresConfig>(config)
            .AddSingleton<IMemoryDb, PostgresMemory>();
    }

    /// <summary>
    /// Inject Postgres as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connString">Postgres connection string</param>
    public static IServiceCollection AddPostgresAsMemoryDb(this IServiceCollection services, string connString)
    {
        var config = new PostgresConfig { ConnectionString = connString };
        return services.AddPostgresAsMemoryDb(config);
    }
}
