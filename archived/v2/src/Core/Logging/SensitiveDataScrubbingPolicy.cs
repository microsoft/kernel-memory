// Copyright (c) Microsoft. All rights reserved.

using Serilog.Core;
using Serilog.Events;

namespace KernelMemory.Core.Logging;

/// <summary>
/// Serilog destructuring policy that scrubs sensitive data in Production environment.
/// In Production, all string values are replaced with a redacted placeholder.
/// In Development/Staging, data is passed through unchanged for debugging.
/// </summary>
public sealed class SensitiveDataScrubbingPolicy : IDestructuringPolicy
{
    /// <summary>
    /// Attempts to destructure a value, scrubbing strings in Production.
    /// </summary>
    /// <param name="value">The value to potentially destructure.</param>
    /// <param name="propertyValueFactory">Factory for creating log event property values.</param>
    /// <param name="result">The destructured value if handled.</param>
    /// <returns>True if this policy handled the value, false to let Serilog process normally.</returns>
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        // Handle null - let Serilog process normally
        if (value is null)
        {
            result = null;
            return false;
        }

        // Only scrub in Production environment
        if (!EnvironmentDetector.IsProduction())
        {
            result = null;
            return false;
        }

        // Only scrub string values - other types pass through
        // Strings are most likely to contain sensitive data like:
        // - API keys, tokens, passwords
        // - User content, PII
        // - File contents, queries
        if (value is string)
        {
            result = new ScalarValue(Constants.LoggingDefaults.RedactedPlaceholder);
            return true;
        }

        // Non-string types pass through unchanged
        // Integers, booleans, DateTimes, Guids are generally safe
        result = null;
        return false;
    }
}
