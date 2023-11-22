// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.Qdrant;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithQdrant(this IKernelMemoryBuilder builder, QdrantConfig config)
    {
        builder.Services.AddQdrantAsVectorDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithQdrant(this IKernelMemoryBuilder builder, string endpoint, string apiKey = "")
    {
        builder.Services.AddQdrantAsVectorDb(endpoint, apiKey);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddQdrantAsVectorDb(this IServiceCollection services, QdrantConfig config)
    {
        return services
            .AddSingleton<QdrantConfig>(config)
            .AddSingleton<IVectorDb, QdrantMemory>();
    }

    public static IServiceCollection AddQdrantAsVectorDb(this IServiceCollection services, string endpoint, string apiKey = "")
    {
        var config = new QdrantConfig { Endpoint = endpoint, APIKey = apiKey };
        return services.AddQdrantAsVectorDb(config);
    }
}
