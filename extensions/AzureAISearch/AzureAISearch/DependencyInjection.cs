// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureAISearchMemoryDb(this IKernelMemoryBuilder builder, AzureAISearchConfig config)
    {
        builder.Services.AddAzureAISearchAsMemoryDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureAISearchMemoryDb(this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        builder.Services.AddAzureAISearchAsMemoryDb(endpoint, apiKey);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
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
