// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI;

// ReSharper disable once CheckNamespace
namespace Microsoft.KernelMemory;

public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Inject a fake embedding generator that will throw an exception if used
    /// </summary>
    /// <param name="builder">KM builder</param>
    public static IKernelMemoryBuilder WithoutEmbeddingGenerator(this IKernelMemoryBuilder builder)
    {
        builder.Services.AddNoEmbeddingGenerator();
        return builder;
    }

    /// <summary>
    /// Inject a fake embedding generator that will throw an exception if used
    /// </summary>
    /// <param name="builder">KM builder</param>
    public static IKernelMemoryBuilder WithoutTextGenerator(this IKernelMemoryBuilder builder)
    {
        builder.Services.AddNoTextGenerator();
        return builder;
    }
}

public static partial class DependencyInjection
{
    /// <summary>
    /// Inject a fake embedding generator that will throw an exception if used
    /// </summary>
    /// <param name="services">.NET services</param>
    public static IServiceCollection AddNoEmbeddingGenerator(this IServiceCollection services)
    {
        return services.AddSingleton<ITextEmbeddingGenerator, NoEmbeddingGenerator>();
    }

    /// <summary>
    /// Inject a fake text generator that will throw an exception if used
    /// </summary>
    /// <param name="services">.NET services</param>
    public static IServiceCollection AddNoTextGenerator(this IServiceCollection services)
    {
        return services.AddSingleton<ITextGenerator, NoTextGenerator>();
    }
}
