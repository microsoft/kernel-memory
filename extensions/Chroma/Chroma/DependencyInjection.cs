// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryDb.Chroma;
using Microsoft.KernelMemory.MemoryStorage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    public static IKernelMemoryBuilder WithChromatMemoryDb(this IKernelMemoryBuilder builder, ChromaConfig config)
    {
        builder.Services.AddChromaAsMemoryDb(config);
        return builder;
    }

    public static IKernelMemoryBuilder WithChromaMemoryDb(this IKernelMemoryBuilder builder, string endpoint)
    {
        builder.Services.AddChromaAsMemoryDb(endpoint);
        return builder;
    }
}

public static partial class DependencyInjection
{
    public static IServiceCollection AddChromaAsMemoryDb(this IServiceCollection services, ChromaConfig config)
    {
        return services
            .AddSingleton<ChromaConfig>(config)
            .AddSingleton<IMemoryDb, ChromaMemory>();
    }

    public static IServiceCollection AddChromaAsMemoryDb(this IServiceCollection services, string endpoint, string apiKey = "")
    {
        var config = new ChromaConfig { Endpoint = endpoint };
        return services.AddChromaAsMemoryDb(config);
    }
}
