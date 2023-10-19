// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.FileSystem.DevTools;
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.MemoryStorage.DevTools;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithSimpleVectorDb(this MemoryClientBuilder builder, SimpleVectorDbConfig? config = null)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(config ?? new SimpleVectorDbConfig());
        return builder;
    }

    public static MemoryClientBuilder WithSimpleVectorDb(this MemoryClientBuilder builder, string directory)
    {
        builder.Services.AddSimpleVectorDbAsVectorDb(directory);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, SimpleVectorDbConfig? config = null)
    {
        return services
            .AddSingleton<SimpleVectorDbConfig>(config ?? new SimpleVectorDbConfig())
            .AddSingleton<ISemanticMemoryVectorDb, SimpleVectorDb>();
    }

    public static IServiceCollection AddSimpleVectorDbAsVectorDb(this IServiceCollection services, string directory)
    {
        var config = new SimpleVectorDbConfig { StorageType = FileSystemTypes.Disk, Directory = directory };
        return services.AddSimpleVectorDbAsVectorDb(config);
    }
}
