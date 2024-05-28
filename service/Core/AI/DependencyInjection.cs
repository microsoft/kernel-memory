// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.AI;

// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions
/// </summary>
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

/// <summary>
/// .NET IServiceCollection dependency injection extensions.
/// </summary>
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
