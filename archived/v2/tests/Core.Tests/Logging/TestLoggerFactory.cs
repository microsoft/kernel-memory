// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Factory for creating loggers that output to XUnit test output.
/// Enables visibility of log messages in test output for debugging failures.
/// This class is in the tests project because it depends on XUnit.
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>
    /// Creates a typed logger that writes to XUnit test output.
    /// Use in tests to capture log output for debugging.
    /// </summary>
    /// <typeparam name="T">The type to create the logger for (provides SourceContext).</typeparam>
    /// <param name="output">XUnit test output helper from the test constructor.</param>
    /// <returns>A configured ILogger of T that writes to test output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when output is null.</exception>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Factory ownership transfers to caller via returned logger")]
    public static ILogger<T> Create<T>(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose() // Enable all levels for test debugging
            .Enrich.FromLogContext()
            .Enrich.With<ActivityEnricher>()
            .WriteTo.TestOutput(output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        var factory = new SerilogLoggerFactory(serilogLogger, dispose: true);
        return factory.CreateLogger<T>();
    }

    /// <summary>
    /// Creates an ILoggerFactory that writes to XUnit test output.
    /// Use when you need to create multiple loggers or inject a factory.
    /// </summary>
    /// <param name="output">XUnit test output helper from the test constructor.</param>
    /// <returns>A configured ILoggerFactory that writes to test output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when output is null.</exception>
    public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.With<ActivityEnricher>()
            .WriteTo.TestOutput(output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        return new SerilogLoggerFactory(serilogLogger, dispose: true);
    }
}
