// Copyright (c) Microsoft. All rights reserved.

using Serilog.Events;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Configuration model for the logging system.
/// Stores log level and file path settings that can be loaded from config files or CLI flags.
/// </summary>
public sealed class LoggingConfig
{
    /// <summary>
    /// Gets or sets the minimum log level for log output.
    /// Defaults to Information which provides useful diagnostics.
    /// </summary>
    public LogEventLevel Level { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Gets or sets the file path for file logging.
    /// When null or empty, file logging is disabled.
    /// Supports both relative paths (from cwd) and absolute paths.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets whether to use JSON format for log output.
    /// When false, uses human-readable format (default).
    /// JSON format is better for log aggregation systems.
    /// </summary>
    public bool UseJsonFormat { get; set; }

    /// <summary>
    /// Gets or sets whether to use async logging.
    /// When false (default), uses synchronous logging suitable for CLI.
    /// Enable for long-running services to improve performance.
    /// </summary>
    public bool UseAsyncLogging { get; set; }

    /// <summary>
    /// Gets a value indicating whether file logging is enabled.
    /// Returns true when FilePath is set to a non-empty value.
    /// </summary>
    public bool IsFileLoggingEnabled => !string.IsNullOrWhiteSpace(this.FilePath);
}
