﻿// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Image.AzureFormRecognizer;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static KernelMemoryBuilder WithAzureFormRecognizer(this KernelMemoryBuilder builder, AzureFormRecognizerConfig config)
    {
        builder.Services.AddAzureFormRecognizer(config);
        return builder;
    }

    public static KernelMemoryBuilder WithAzureFormRecognizer(this KernelMemoryBuilder builder, string endpoint, string apiKey)
    {
        return builder.WithAzureFormRecognizer(new AzureFormRecognizerConfig
        {
            Auth = AzureFormRecognizerConfig.AuthTypes.APIKey,
            Endpoint = endpoint,
            APIKey = apiKey
        });
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureFormRecognizer(this IServiceCollection services, AzureFormRecognizerConfig config)
    {
        return services
            .AddSingleton<AzureFormRecognizerConfig>(config)
            .AddSingleton<IOcrEngine, AzureFormRecognizerEngine>();
    }

    public static IServiceCollection AddAzureFormRecognizer(this IServiceCollection services, string endpoint, string apiKey)
    {
        var config = new AzureFormRecognizerConfig { Endpoint = endpoint, APIKey = apiKey, Auth = AzureFormRecognizerConfig.AuthTypes.APIKey };
        return services.AddAzureFormRecognizer(config);
    }
}
