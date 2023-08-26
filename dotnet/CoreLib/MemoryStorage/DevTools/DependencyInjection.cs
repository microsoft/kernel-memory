// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.MemoryStorage.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithSimpleVectorDb(this MemoryClientBuilder builder, SimpleVectorDbConfig config)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(config);
        return builder;
    }

    public static MemoryClientBuilder WithSimpleVectorDb(this MemoryClientBuilder builder, string path)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(path);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, SimpleVectorDbConfig config)
    {
        return services
            .AddSingleton<SimpleVectorDbConfig>(config)
            .AddSingleton<ISemanticMemoryVectorDb, SimpleVectorDb>();
    }

    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, string path)
    {
        var config = new SimpleVectorDbConfig { Directory = path };
        return services.AddSimpleVectorDbAsVectorDb(config);
    }
}
