// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryDb.Redis;
using StackExchange.Redis;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// DI pipelines for Redis Memory.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Adds RedisMemory as a service.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="connString">The Redis connection string based on <see href="https://stackexchange.github.io/StackExchange.Redis/Configuration">StackExchange.Redis' connection string</see></param>
    public static IKernelMemoryBuilder WithRedisMemoryDb(
        this IKernelMemoryBuilder builder,
        string connString)
    {
        builder.Services.AddRedisAsMemoryDb(new RedisConfig { ConnectionString = connString });
        return builder;
    }

    /// <summary>
    /// Adds RedisMemory as a service.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="redisConfig">Redis configuration.</param>
    public static IKernelMemoryBuilder WithRedisMemoryDb(
        this IKernelMemoryBuilder builder,
        RedisConfig redisConfig)
    {
        builder.Services.AddRedisAsMemoryDb(redisConfig);
        return builder;
    }
}

/// <summary>
/// setup Redis memory within the semantic kernel
/// </summary>
public static partial class DependencyInjection
{
    /// <summary>
    /// Adds RedisMemory as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="redisConfig">Redis configuration.</param>
    public static IServiceCollection AddRedisAsMemoryDb(
        this IServiceCollection services,
        RedisConfig redisConfig)
    {
        return services
            .AddSingleton(redisConfig)
            .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfig.ConnectionString))
            .AddSingleton<IMemoryDb, RedisMemory>();
    }
}
