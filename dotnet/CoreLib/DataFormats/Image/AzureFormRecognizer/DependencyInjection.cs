// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.DataFormats.Image;
using Microsoft.SemanticMemory.DataFormats.Image.AzureFormRecognizer;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithAzureFormRecognizer(this MemoryClientBuilder builder, AzureFormRecognizerConfig config)
    {
        builder.Services.AddAzureFormRecognizer(config);

        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureFormRecognizer(this IServiceCollection services, AzureFormRecognizerConfig config)
    {
        return services
            .AddSingleton<AzureFormRecognizerConfig>(config)
            .AddTransient<IOcrEngine, AzureFormRecognizerEngine>();
    }
}
