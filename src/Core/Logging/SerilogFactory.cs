// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Factory for creating Serilog-based ILoggerFactory instances.
/// Configures Serilog with console, file, and optional async sinks
/// based on the provided LoggingConfig.
/// </summary>
public static class SerilogFactory
{
    /// <summary>
    /// Creates an ILoggerFactory configured with Serilog based on the provided config.
    /// The factory integrates with Microsoft.Extensions.Logging for DI compatibility.
    /// </summary>
    /// <param name="config">Logging configuration specifying level, file path, and options.</param>
    /// <returns>A configured ILoggerFactory that creates Serilog-backed loggers.</returns>
    public static ILoggerFactory CreateLoggerFactory(LoggingConfig config)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(config.Level)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "KernelMemory");

        // Add correlation IDs from System.Diagnostics.Activity
        loggerConfig = loggerConfig.Enrich.With<ActivityEnricher>();

        // Add sensitive data scrubbing policy
        loggerConfig = loggerConfig.Destructure.With<SensitiveDataScrubbingPolicy>();

        // Configure console output (stderr) for warnings and errors
        loggerConfig = loggerConfig.WriteTo.Console(
            restrictedToMinimumLevel: LoggingConstants.DefaultConsoleLogLevel,
            outputTemplate: LoggingConstants.ConsoleOutputTemplate,
            formatProvider: CultureInfo.InvariantCulture,
            standardErrorFromLevel: LogEventLevel.Verbose);

        // Configure file output if path is specified
        if (config.IsFileLoggingEnabled)
        {
            loggerConfig = ConfigureFileLogging(loggerConfig, config);
        }

        var serilogLogger = loggerConfig.CreateLogger();

        // Create Microsoft.Extensions.Logging factory backed by Serilog
        return new SerilogLoggerFactory(serilogLogger, dispose: true);
    }

    /// <summary>
    /// Configures file logging with rotation settings.
    /// Supports both sync and async modes, and human-readable or JSON formats.
    /// </summary>
    /// <param name="loggerConfig">The logger configuration to extend.</param>
    /// <param name="config">Logging configuration with file settings.</param>
    /// <returns>The extended logger configuration.</returns>
    private static LoggerConfiguration ConfigureFileLogging(
        LoggerConfiguration loggerConfig,
        LoggingConfig config)
    {
        // Ensure directory exists for the log file
        var filePath = config.FilePath!;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (config.UseAsyncLogging)
        {
            // Async logging for better performance in services
            if (config.UseJsonFormat)
            {
                loggerConfig = loggerConfig.WriteTo.Async(a => a.File(
                    new CompactJsonFormatter(),
                    filePath,
                    fileSizeLimitBytes: LoggingConstants.DefaultFileSizeLimitBytes,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: LoggingConstants.DefaultRetainedFileCountLimit));
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.Async(a => a.File(
                    filePath,
                    outputTemplate: LoggingConstants.HumanReadableOutputTemplate,
                    formatProvider: CultureInfo.InvariantCulture,
                    fileSizeLimitBytes: LoggingConstants.DefaultFileSizeLimitBytes,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: LoggingConstants.DefaultRetainedFileCountLimit));
            }
        }
        else
        {
            // Sync logging for CLI (short-lived commands)
            if (config.UseJsonFormat)
            {
                loggerConfig = loggerConfig.WriteTo.File(
                    new CompactJsonFormatter(),
                    filePath,
                    fileSizeLimitBytes: LoggingConstants.DefaultFileSizeLimitBytes,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: LoggingConstants.DefaultRetainedFileCountLimit);
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.File(
                    filePath,
                    outputTemplate: LoggingConstants.HumanReadableOutputTemplate,
                    formatProvider: CultureInfo.InvariantCulture,
                    fileSizeLimitBytes: LoggingConstants.DefaultFileSizeLimitBytes,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: LoggingConstants.DefaultRetainedFileCountLimit);
            }
        }

        return loggerConfig;
    }
}
