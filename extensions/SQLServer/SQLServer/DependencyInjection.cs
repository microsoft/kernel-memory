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
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="useNativeVectorSearch">Whether to use native vector search or not.</param>
    /// <param name="vectorSize">When <paramref name="useNativeVectorSearch"/> is <see langword="true"/>, it is the vector size used by the VECTOR data type.</param>
    /// <remarks>
    /// Currently, the native Vector search is available on Azure SQL Database only.
    /// See <a href="https://learn.microsoft.com/sql/relational-databases/vectors/vectors-sql-server">Overview of vectors in the SQL Database Engine</a> for more information about native Vectors support.
    /// </remarks>
    public static IKernelMemoryBuilder WithSqlServerMemoryDb(
        this IKernelMemoryBuilder builder,
        string connectionString,
        bool useNativeVectorSearch = false,
        int vectorSize = SqlServerConfig.DefaultVectorSize)
    {
        builder.Services.AddSqlServerAsMemoryDb(connectionString, useNativeVectorSearch, vectorSize);
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
        config.Validate();

        return services
            .AddSingleton<SqlServerConfig>(config)
            .AddSingleton<IMemoryDb, SqlServerMemory>();
    }

    /// <summary>
    /// Inject SQL Server as the default implementation of IMemoryDb
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="useNativeVectorSearch">Whether to use native vector search or not. Currently, the native Vector search is in Early Access Preview (EAP) and is available on Azure SQL Database and Managed Instance only.</param>
    /// <param name="vectorSize">When <paramref name="useNativeVectorSearch"/> is <see langword="true"/>, it is the vector size used by the VECTOR SQL Server type.</param>
    public static IServiceCollection AddSqlServerAsMemoryDb(
        this IServiceCollection services,
        string connectionString,
        bool useNativeVectorSearch = false,
        int vectorSize = SqlServerConfig.DefaultVectorSize)
    {
        var config = new SqlServerConfig
        {
            ConnectionString = connectionString,
            UseNativeVectorSearch = useNativeVectorSearch,
            VectorSize = vectorSize
        };

        return services.AddSqlServerAsMemoryDb(config);
    }
}
