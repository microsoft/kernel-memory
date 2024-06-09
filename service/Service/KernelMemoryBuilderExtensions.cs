// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Service;

// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// Kernel Memory builder extensions for ASP.NET apps using settings in appsettings.json
/// and using IConfiguration. The following methods allow to fully configure KM via
/// IConfiguration, without having to change the code using KernelMemoryBuilder and recompile.
/// </summary>
public static partial class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Configure the builder using settings stored in the specified directory.
    /// If the directory is empty, use the current assembly folder
    /// </summary>
    /// <param name="builder">KernelMemory builder instance</param>
    /// <param name="settingsDirectory">Directory containing appsettings.json (incl. dev/prod)</param>
    public static IKernelMemoryBuilder FromAppSettings(
        this IKernelMemoryBuilder builder,
        string? settingsDirectory = null)
    {
        return new ServiceConfiguration(settingsDirectory).PrepareBuilder(builder);
    }

    /// <summary>
    /// Configure the builder using settings from the given IConfiguration instance.
    /// </summary>
    /// <param name="builder">KernelMemory builder instance</param>
    /// <param name="servicesConfiguration">KM configuration + Dependencies configuration</param>
    public static IKernelMemoryBuilder FromIConfiguration(
        this IKernelMemoryBuilder builder,
        IConfiguration servicesConfiguration)
    {
        return new ServiceConfiguration(servicesConfiguration).PrepareBuilder(builder);
    }

    /// <summary>
    /// Configure the builder using settings from the given KernelMemoryConfig and IConfiguration instances.
    /// </summary>
    /// <param name="builder">KernelMemory builder instance</param>
    /// <param name="memoryConfiguration">KM configuration</param>
    /// <param name="servicesConfiguration">Dependencies configuration, e.g. queue, embedding, storage, etc.</param>
    public static IKernelMemoryBuilder FromMemoryConfiguration(
        this IKernelMemoryBuilder builder,
        KernelMemoryConfig memoryConfiguration,
        IConfiguration servicesConfiguration)
    {
        return new ServiceConfiguration(servicesConfiguration, memoryConfiguration).PrepareBuilder(builder);
    }
}
