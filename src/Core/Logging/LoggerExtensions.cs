// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Extension methods for ILogger providing CallerMemberName-based method logging.
/// These helpers automatically capture the calling method name for entry/exit logging.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs method entry at Debug level with automatic method name capture.
    /// Use this at the beginning of public methods for diagnostics.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="param1">Optional first parameter to log.</param>
    /// <param name="param2">Optional second parameter to log.</param>
    /// <param name="param3">Optional third parameter to log.</param>
    /// <param name="methodName">Automatically captured method name (do not pass explicitly).</param>
    public static void LogMethodEntry(
        this ILogger logger,
        object? param1 = null,
        object? param2 = null,
        object? param3 = null,
        [CallerMemberName] string methodName = "")
    {
        // Skip logging if Debug level is disabled for performance
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        // Log with structured parameters for queryability
        logger.LogDebug(
            "{MethodName} called with {Param1}, {Param2}, {Param3}",
            methodName,
            param1,
            param2,
            param3);
    }

    /// <summary>
    /// Logs method exit at Debug level with automatic method name capture.
    /// Use this before returning from public methods for diagnostics.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="result">Optional result value to log.</param>
    /// <param name="methodName">Automatically captured method name (do not pass explicitly).</param>
    public static void LogMethodExit(
        this ILogger logger,
        object? result = null,
        [CallerMemberName] string methodName = "")
    {
        // Skip logging if Debug level is disabled for performance
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        // Log with structured parameters for queryability
        logger.LogDebug(
            "{MethodName} completed with {Result}",
            methodName,
            result);
    }
}
