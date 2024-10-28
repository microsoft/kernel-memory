// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.Diagnostics;

#pragma warning disable CA2254 // by design
public static class SensitiveDataLogger
{
    private static bool s_enabled = false;

    public static bool Enabled
    {
        get => s_enabled;
        set
        {
            if (s_enabled == value) { return; }

            if (value) { EnsureDevelopmentEnvironment(); }

            s_enabled = value;
        }
    }

    public static LogLevel LoggingLevel { get; set; } = LogLevel.Information;

    public static void LogSensitive(this ILogger logger, string? message, params object?[] args)
    {
        if (!Enabled) { return; }

        logger.Log(LoggingLevel, $"[PII] {message}", args);
    }

    public static void LogSensitive(
        this ILogger logger,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        if (!Enabled) { return; }

        logger.Log(LoggingLevel, exception, message, args);
    }

    public static void LogSensitive(
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        if (!Enabled) { return; }

        logger.Log(LoggingLevel, eventId, exception, message, args);
    }

    public static void LogSensitive(
        this ILogger logger,
        EventId eventId,
        string? message,
        params object?[] args)
    {
        if (!Enabled) { return; }

        logger.Log(LoggingLevel, eventId, message, args);
    }

    private static void EnsureDevelopmentEnvironment()
    {
        const string AspNetCoreEnvVar = "ASPNETCORE_ENVIRONMENT";
        const string DotNetEnvVar = "DOTNET_ENVIRONMENT";

        // The ASPNETCORE_ENVIRONMENT environment variable has the precedence. If it does not exist, checks for DOTNET_ENVIRONMENT.
        var env = Environment.GetEnvironmentVariable(AspNetCoreEnvVar) ?? Environment.GetEnvironmentVariable(DotNetEnvVar);
        if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Sensitive data logging can be enabled only in a development environment. Check {AspNetCoreEnvVar} or {DotNetEnvVar} env vars.");
        }
    }
}
