// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.SQLServer;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Kernel Memory Builder extension method to add SQL Server memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="config">SQL Server configuration</param>
    public static IKernelMemoryBuilder WithSqlServerMemoryDb(
        this IKernelMemoryBuilder builder,
        SqlServerConfig config)
    {
        builder.Services.AddSqlServerAsMemoryDb(config);
        return builder;
    }

    /// <summary>
    /// Kernel Memory Builder extension method to add SQL Server memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="connString">SQL Server connection string</param>
    /// <param name="useNativeVectorSearch">Whether to use native vector search or not</param>
    public static IKernelMemoryBuilder WithSqlServerMemoryDb(
        this IKernelMemoryBuilder builder,
        string connString,
        bool useNativeVectorSearch = false)
    {
        builder.Services.AddSqlServerAsMemoryDb(connString, useNativeVectorSearch);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject SQL Server as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">SQL Server configuration</param>
    public static IServiceCollection AddSqlServerAsMemoryDb(
        this IServiceCollection services,
        SqlServerConfig config)
    {
        return services
            .AddSingleton<SqlServerConfig>(config)
            .AddSingleton<IMemoryDb, SqlServerMemory>();
    }

    /// <summary>
    /// Inject SQL Server as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connString">SQL Server connection string</param>
    /// <param name="useNativeVectorSearch">Whether to use native vector search or not</param>
    public static IServiceCollection AddSqlServerAsMemoryDb(
        this IServiceCollection services,
        string connString,
        bool useNativeVectorSearch = false)
    {
        var config = new SqlServerConfig { ConnectionString = connString, UseNativeVectorSearch = useNativeVectorSearch };
        return services.AddSqlServerAsMemoryDb(config);
    }
}
