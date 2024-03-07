// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.AzureAIDocIntel;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureAIDocIntel(
        this IKernelMemoryBuilder builder, AzureAIDocIntelConfig config)
    {
        config.Validate();
        builder.Services.AddAzureAIDocIntel(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureAIDocIntel(
        this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        var config = new AzureAIDocIntelConfig
        {
            Auth = AzureAIDocIntelConfig.AuthTypes.APIKey,
            Endpoint = endpoint,
            APIKey = apiKey
        };
        config.Validate();

        return builder.WithAzureAIDocIntel(config);
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureAIDocIntel(
        this IServiceCollection services, AzureAIDocIntelConfig config)
    {
        config.Validate();
        return services
            .AddSingleton<AzureAIDocIntelConfig>(config)
            .AddSingleton<IOcrEngine, AzureAIDocIntelEngine>();
    }

    public static IServiceCollection AddAzureAIDocIntel(
        this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureAIDocIntelConfig
        {
            Endpoint = endpoint,
            APIKey = apiKey,
            Auth = AzureAIDocIntelConfig.AuthTypes.APIKey
        };
        config.Validate();
        return services.AddAzureAIDocIntel(config);
    }
}
