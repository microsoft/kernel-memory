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
        get
        {
            return s_enabled;
        }
        set
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
#pragma warning disable CA2201
                throw new ApplicationException("Sensitive data logging can be enabled only in a development environment. Check ASPNETCORE_ENVIRONMENT env var.");
#pragma warning restore CA0000
            }

            s_enabled = value && string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
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
}
