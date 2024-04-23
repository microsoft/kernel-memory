// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// DI extension methods to inject KM into WebApplication
/// </summary>
public static partial class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Build and add KM Memory singleton instance to your web app.
    /// </summary>
    /// <param name="appBuilder">Hosting application builder</param>
    /// <param name="configureMemoryBuilder">Optional configuration steps for the memory builder</param>
    /// <param name="configureMemory">Optional configuration steps for the memory instance</param>
    public static WebApplicationBuilder AddKernelMemory(
        this WebApplicationBuilder appBuilder,
        Action<IKernelMemoryBuilder>? configureMemoryBuilder = null,
        Action<IKernelMemory>? configureMemory = null)
    {
        // Prepare memory builder, sharing the service collection used by the hosting service
        var memoryBuilder = new KernelMemoryBuilder(appBuilder.Services);

        // Optional configuration provided by the user
        configureMemoryBuilder?.Invoke(memoryBuilder);

        var memory = memoryBuilder.Build();

        // Optional configuration provided by the user
        configureMemory?.Invoke(memory);

        appBuilder.Services.AddSingleton<IKernelMemory>(memory);

        return appBuilder;
    }
}
