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
    /// Kernel Memory Builder extension method to add SqlServer memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="config">SqlServer configuration</param>
    public static IKernelMemoryBuilder WithSqlServerMemoryDb(
        this IKernelMemoryBuilder builder,
        SqlServerConfig config)
    {
        builder.Services.AddSqlServerAsMemoryDb(config);
        return builder;
    }

    /// <summary>
    /// Kernel Memory Builder extension method to add SqlServer memory connector.
    /// </summary>
    /// <param name="builder">KM builder instance</param>
    /// <param name="connString">SqlServer connection string</param>
    public static IKernelMemoryBuilder WithSqlServerMemoryDb(
        this IKernelMemoryBuilder builder,
        string connString)
    {
        builder.Services.AddSqlServerAsMemoryDb(connString);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Inject SqlServer as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Postgres configuration</param>
    public static IServiceCollection AddSqlServerAsMemoryDb(
        this IServiceCollection services,
        SqlServerConfig config)
    {
        return services
            .AddSingleton<SqlServerConfig>(config)
            .AddSingleton<IMemoryDb, SqlServerMemory>();
    }

    /// <summary>
    /// Inject SqlServer as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connString">SQL Server connection string</param>
    public static IServiceCollection AddSqlServerAsMemoryDb(
        this IServiceCollection services,
        string connString)
    {
        var config = new SqlServerConfig { ConnectionString = connString };
        return services.AddSqlServerAsMemoryDb(config);
    }
}
