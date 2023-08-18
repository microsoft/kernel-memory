// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.Core.AppBuilders;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.Qdrant;

public static class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithQdrant(this MemoryClientBuilder builder, QdrantConfig config)
    {
        builder.Services.AddQdrantAsVectorDb(config);
        return builder;
    }

    public static MemoryClientBuilder WithQdrant(this MemoryClientBuilder builder, string endpoint, string apiKey = "")
    {
        builder.Services.AddQdrantAsVectorDb(endpoint, apiKey);
        return builder;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddQdrantAsVectorDb(this IServiceCollection services, QdrantConfig config)
    {
        return services
            .AddSingleton<QdrantConfig>(config)
            .AddSingleton<ISemanticMemoryVectorDb, QdrantMemory>();
    }

    public static IServiceCollection AddQdrantAsVectorDb(this IServiceCollection services, string endpoint, string apiKey = "")
    {
        var config = new QdrantConfig { Endpoint = endpoint, APIKey = apiKey };
        return services.AddQdrantAsVectorDb(config);
    }
}
