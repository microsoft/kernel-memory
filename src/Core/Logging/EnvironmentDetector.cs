// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Core.Logging;

/// <summary>
/// Detects the current runtime environment from environment variables.
/// Environment detection is critical for security decisions like sensitive data scrubbing.
/// </summary>
public static class EnvironmentDetector
{
    /// <summary>
    /// Gets the current environment name.
    /// Checks DOTNET_ENVIRONMENT first, falls back to ASPNETCORE_ENVIRONMENT,
    /// then defaults to Development if neither is set.
    /// </summary>
    /// <returns>The environment name (e.g., "Development", "Production", "Staging").</returns>
    public static string GetEnvironment()
    {
        // Check DOTNET_ENVIRONMENT first (takes precedence)
        var dotNetEnv = Environment.GetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(dotNetEnv))
        {
            return dotNetEnv;
        }

        // Fall back to ASPNETCORE_ENVIRONMENT
        var aspNetEnv = Environment.GetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(aspNetEnv))
        {
            return aspNetEnv;
        }

        // Default to Development for safety (full logging)
        return Constants.LoggingDefaults.DefaultEnvironment;
    }

    /// <summary>
    /// Checks if the current environment is Production.
    /// In Production, sensitive data is scrubbed from logs.
    /// </summary>
    /// <returns>True if running in Production environment.</returns>
    public static bool IsProduction()
    {
        return string.Equals(
            GetEnvironment(),
            Constants.LoggingDefaults.ProductionEnvironment,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the current environment is Development.
    /// In Development, full logging is enabled for debugging.
    /// </summary>
    /// <returns>True if running in Development environment.</returns>
    public static bool IsDevelopment()
    {
        return string.Equals(
            GetEnvironment(),
            Constants.LoggingDefaults.DefaultEnvironment,
            StringComparison.OrdinalIgnoreCase);
    }
}
