// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.AzureAISearch;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureAISearch(this IKernelMemoryBuilder builder, AzureAISearchConfig config)
    {
        builder.Services.AddAzureAISearchAsMemoryDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureAISearch(this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        builder.Services.AddAzureAISearchAsMemoryDb(endpoint, apiKey);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureAISearchAsMemoryDb(this IServiceCollection services, AzureAISearchConfig config)
    {
        return services
            .AddSingleton<AzureAISearchConfig>(config)
            .AddSingleton<IMemoryDb, AzureAISearchMemory>();
    }

    public static IServiceCollection AddAzureAISearchAsMemoryDb(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureAISearchConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureAISearchConfig.AuthTypes.APIKey };
        return services.AddAzureAISearchAsMemoryDb(config);
    }
}
