// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryDb.Qdrant;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithQdrantMemoryDb(this IKernelMemoryBuilder builder, QdrantConfig config)
    {
        builder.Services.AddQdrantAsMemoryDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithQdrantMemoryDb(this IKernelMemoryBuilder builder, string endpoint, string apiKey = "")
    {
        builder.Services.AddQdrantAsMemoryDb(endpoint, apiKey);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddQdrantAsMemoryDb(this IServiceCollection services, QdrantConfig config)
    {
        return services
            .AddSingleton<QdrantConfig>(config)
            .AddSingleton<IMemoryDb, QdrantMemory>();
    }

    public static IServiceCollection AddQdrantAsMemoryDb(this IServiceCollection services, string endpoint, string apiKey = "")
    {
        var config = new QdrantConfig { Endpoint = endpoint, APIKey = apiKey };
        return services.AddQdrantAsMemoryDb(config);
    }
}
