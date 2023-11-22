// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.AzureCognitiveSearch;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureCognitiveSearch(this IKernelMemoryBuilder builder, AzureCognitiveSearchConfig config)
    {
        builder.Services.AddAzureCognitiveSearchAsVectorDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureCognitiveSearch(this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        builder.Services.AddAzureCognitiveSearchAsVectorDb(endpoint, apiKey);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, AzureCognitiveSearchConfig config)
    {
        return services
            .AddSingleton<AzureCognitiveSearchConfig>(config)
            .AddSingleton<IVectorDb, AzureCognitiveSearchMemory>();
    }

    public static IServiceCollection AddAzureCognitiveSearchAsVectorDb(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureCognitiveSearchConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureCognitiveSearchConfig.AuthTypes.APIKey };
        return services.AddAzureCognitiveSearchAsVectorDb(config);
    }
}
