// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.Extensions.Configuration;
#pragma warning restore IDE0130

/// <summary>
/// Microsoft.Extensions.Configuration.IConfiguration extension methods.
/// </summary>
public static partial class ConfigurationExtensions
{
    /// <summary>
    /// Populate the instance with data from the configuration, at the given key,
    /// returning the configuration instance to chain multiple calls.
    /// </summary>
    /// <param name="configuration">Configuration object</param>
    /// <param name="key">Key pointing to the data to load</param>
    /// <param name="instance">Object to populate</param>
    /// <returns>Configuration instance</returns>
    public static IConfiguration BindSection(this IConfiguration configuration, string key, object instance)
    {
        configuration.GetSection(key).Bind(instance);
        return configuration;
    }
}
