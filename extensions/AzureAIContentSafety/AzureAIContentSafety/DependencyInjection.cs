// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Safety.AzureAIContentSafety;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
[Experimental("KMEXP05")]
public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithAzureAIContentSafetyModeration(this IKernelMemoryBuilder builder, AzureAIContentSafetyConfig config)
    {
        builder.Services.AddAzureAIContentSafetyModeration(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithAzureAIContentSafetyModeration(this IKernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        builder.Services.AddAzureAIContentSafetyModeration(endpoint, apiKey);
        return builder;
    }
}

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
[Experimental("KMEXP05")]
public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureAIContentSafetyModeration(this IServiceCollection services, AzureAIContentSafetyConfig config)
    {
        config.Validate();
        return services
            .AddSingleton<AzureAIContentSafetyConfig>(config)
            .AddSingleton<IContentModeration, AzureAIContentSafetyModeration>();
    }

    public static IServiceCollection AddAzureAIContentSafetyModeration(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureAIContentSafetyConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureAIContentSafetyConfig.AuthTypes.APIKey };
        config.Validate();
        return services.AddAzureAIContentSafetyModeration(config);
    }
}
