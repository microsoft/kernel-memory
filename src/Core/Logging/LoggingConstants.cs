// Copyright (c) Microsoft. All rights reserved.

using Serilog.Events;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Centralized constants for the logging system.
/// All magic values related to logging are defined here for maintainability.
/// </summary>
public static class LoggingConstants
{
    /// <summary>
    /// Default maximum file size before rotation (100MB).
    /// Balances history retention with disk usage.
    /// </summary>
    public const long DefaultFileSizeLimitBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Default number of log files to retain (30 files).
    /// Approximately 1 month of daily logs or ~3GB max storage.
    /// </summary>
    public const int DefaultRetainedFileCountLimit = 30;

    /// <summary>
    /// Default minimum log level for file output.
    /// Information level provides useful diagnostics without excessive verbosity.
    /// </summary>
    public const LogEventLevel DefaultFileLogLevel = LogEventLevel.Information;

    /// <summary>
    /// Default minimum log level for console/stderr output.
    /// Only warnings and errors appear on stderr by default.
    /// </summary>
    public const LogEventLevel DefaultConsoleLogLevel = LogEventLevel.Warning;

    /// <summary>
    /// Environment variable for .NET runtime environment detection.
    /// Takes precedence over ASPNETCORE_ENVIRONMENT.
    /// </summary>
    public const string DotNetEnvironmentVariable = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// Fallback environment variable for ASP.NET Core applications.
    /// Used when DOTNET_ENVIRONMENT is not set.
    /// </summary>
    public const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// Default environment when no environment variable is set.
    /// Defaults to Development for developer safety (full logging enabled).
    /// </summary>
    public const string DefaultEnvironment = "Development";

    /// <summary>
    /// Production environment name for comparison.
    /// Sensitive data is scrubbed only in Production.
    /// </summary>
    public const string ProductionEnvironment = "Production";

    /// <summary>
    /// Placeholder text for redacted sensitive data.
    /// Used to indicate data was intentionally removed from logs.
    /// </summary>
    public const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Human-readable output template for log messages.
    /// Includes timestamp, level, source context, message, and optional exception.
    /// </summary>
    public const string HumanReadableOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Compact output template for console (stderr) output.
    /// Shorter format suitable for CLI error reporting.
    /// </summary>
    public const string ConsoleOutputTemplate =
        "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Empty trace ID value (32 zeros) used when no Activity is present.
    /// Indicates no distributed tracing context is available.
    /// </summary>
    public const string EmptyTraceId = "00000000000000000000000000000000";

    /// <summary>
    /// Empty span ID value (16 zeros) used when no Activity is present.
    /// Indicates no distributed tracing context is available.
    /// </summary>
    public const string EmptySpanId = "0000000000000000";
}
