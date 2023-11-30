// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Image.AzureAIDocIntel;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureAIDocIntel(this IKernelMemoryBuilder builder, AzureAIDocIntelConfig config)
    {
        builder.Services.AddAzureAIDocIntel(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureAIDocIntel(this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        return builder.WithAzureAIDocIntel(new AzureAIDocIntelConfig
        {
            Auth = AzureAIDocIntelConfig.AuthTypes.APIKey,
            Endpoint = endpoint,
            APIKey = apiKey
        });
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureAIDocIntel(this IServiceCollection services, AzureAIDocIntelConfig config)
    {
        return services
            .AddSingleton<AzureAIDocIntelConfig>(config)
            .AddSingleton<IOcrEngine, AzureAIDocIntelEngine>();
    }

    public static IServiceCollection AddAzureAIDocIntel(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureAIDocIntelConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureAIDocIntelConfig.AuthTypes.APIKey };
        return services.AddAzureAIDocIntel(config);
    }
}
