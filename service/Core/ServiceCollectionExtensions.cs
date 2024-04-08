// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.KernelMemory;

/// <summary>
/// Service Collection extensions for Kernel Memory.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Kernel Memory services to the specified <see cref="IServiceCollection"/> and registers a singleton <see cref="IKernelMemory"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="setupAction">An optional action to configure the <see cref="IKernelMemory">Kernel Memory builder</see>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddKernelMemory(this IServiceCollection services, Action<IKernelMemoryBuilder>? setupAction = null)
    {
        var kernelMemoryBuilder = new KernelMemoryBuilder(services);
        setupAction?.Invoke(kernelMemoryBuilder);

        var kernelMemory = kernelMemoryBuilder.Build();
        services.AddSingleton<IKernelMemory>(kernelMemory);

        return services;
    }

    /// <summary>
    /// Adds Kernel Memory services to the specified <see cref="IServiceCollection"/> and registers both a singleton <see cref="IKernelMemory"/> service and the implementation of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="setupAction">An optional action to configure the <see cref="IKernelMemory">Kernel Memory builder</see>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddKernelMemory<T>(this IServiceCollection services, Action<IKernelMemoryBuilder>? setupAction = null)
        where T : class, IKernelMemory
    {
        var kernelMemoryBuilder = new KernelMemoryBuilder(services);
        setupAction?.Invoke(kernelMemoryBuilder);

        var kernelMemory = kernelMemoryBuilder.Build<T>();

        services.AddSingleton(kernelMemory);
        services.AddSingleton<IKernelMemory>(provider => provider.GetRequiredService<T>());

        return services;
    }
}
