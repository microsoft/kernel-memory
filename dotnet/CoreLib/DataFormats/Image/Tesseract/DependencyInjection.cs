// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory.DataFormats.Image;
using Microsoft.SemanticMemory.DataFormats.Image.Tesseract;

// ReSharper disable once CheckNamespace
namespace Microsoft.SemanticMemory;

public static partial class MemoryClientBuilderExtensions
{
    public static MemoryClientBuilder WithTesseractOCR(this MemoryClientBuilder builder, TesseractConfig config)
    {
        builder.Services.AddTesseractOCR(config);

        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddTesseractOCR(this IServiceCollection services, TesseractConfig config)
    {
        return services
            .AddSingleton<TesseractConfig>(config)
            .AddTransient<IOcrEngine, TesseractOcrEngine>();
    }
}
