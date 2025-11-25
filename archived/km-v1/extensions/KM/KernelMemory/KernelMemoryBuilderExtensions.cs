// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Internals;

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
    /// Configure the builder using settings from the given IConfiguration instance.
    /// </summary>
    /// <param name="builder">KernelMemory builder instance</param>
    /// <param name="appSettings">App settings, which might include KM settings</param>
    /// <param name="memoryConfig">Optional KM settings, overriding those in appsettings</param>
    public static IKernelMemoryBuilder ConfigureDependencies(
        this IKernelMemoryBuilder builder,
        IConfiguration appSettings,
        KernelMemoryConfig? memoryConfig = null)
    {
        if (appSettings is null)
        {
            throw new ConfigurationException("The given app settings configuration is NULL");
        }

        if (memoryConfig is null)
        {
            memoryConfig = appSettings.GetSection(KernelMemoryComposer.ConfigRoot).Get<KernelMemoryConfig>();
        }

        if (memoryConfig is null)
        {
            throw new ConfigurationException($"Unable to load Kernel Memory settings from the given configuration. " +
                                             $"There should be a '{KernelMemoryComposer.ConfigRoot}' root node, " +
                                             $"with data mapping to '{nameof(KernelMemoryConfig)}'");
        }

        var composer = new KernelMemoryComposer(builder, appSettings, memoryConfig);
        composer.ConfigureBuilder();

        return builder;
    }
}
