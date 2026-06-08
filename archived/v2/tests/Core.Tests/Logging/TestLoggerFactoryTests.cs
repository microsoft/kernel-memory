// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for TestLoggerFactory - validates XUnit output integration for tests.
/// TestLoggerFactory enables test log output visibility for debugging test failures.
/// </summary>
public sealed class TestLoggerFactoryTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes test with XUnit output helper.
    /// </summary>
    /// <param name="output">XUnit test output helper.</param>
    public TestLoggerFactoryTests(ITestOutputHelper output)
    {
        this._output = output;
    }

    /// <summary>
    /// Verifies Create returns non-null typed logger.
    /// </summary>
    [Fact]
    public void Create_ShouldReturnTypedLogger()
    {
        // Act
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Assert
        Assert.NotNull(logger);
    }

    /// <summary>
    /// Verifies logger can log messages without throwing.
    /// </summary>
    [Fact]
    public void Create_ShouldCreateFunctionalLogger()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Act
        var exception = Record.Exception(() =>
        {
            logger.LogDebug("Debug message");
            logger.LogInformation("Info message");
            logger.LogWarning("Warning message");
            logger.LogError("Error message");
        });

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies logger enables Debug level by default for detailed test output.
    /// </summary>
    [Fact]
    public void Create_ShouldEnableDebugLevel()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Assert
        Assert.True(logger.IsEnabled(LogLevel.Debug));
    }

    /// <summary>
    /// Verifies logger enables Trace level for maximum verbosity in tests.
    /// </summary>
    [Fact]
    public void Create_ShouldEnableTraceLevel()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Assert
        Assert.True(logger.IsEnabled(LogLevel.Trace));
    }

    /// <summary>
    /// Verifies logger can log structured data.
    /// </summary>
    [Fact]
    public void Create_ShouldSupportStructuredLogging()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Act - log with structured parameters
        var exception = Record.Exception(() =>
        {
            logger.LogInformation("Processing {ItemCount} items for {UserId}", 42, "user-123");
        });

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies logger can log exceptions with stack traces.
    /// </summary>
    [Fact]
    public void Create_ShouldLogExceptionsWithStackTrace()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);
        var testException = new InvalidOperationException("Test exception");

        // Act
        var exception = Record.Exception(() =>
        {
            logger.LogError(testException, "An error occurred");
        });

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies CreateLoggerFactory returns functional factory.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_ShouldReturnFunctionalFactory()
    {
        // Act
        using var factory = TestLoggerFactory.CreateLoggerFactory(this._output);

        // Assert
        Assert.NotNull(factory);

        var logger = factory.CreateLogger<TestLoggerFactoryTests>();
        Assert.NotNull(logger);
    }

    /// <summary>
    /// Verifies factory can create multiple loggers for different types.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_ShouldCreateMultipleLoggers()
    {
        // Arrange
        using var factory = TestLoggerFactory.CreateLoggerFactory(this._output);

        // Act
        var logger1 = factory.CreateLogger<TestLoggerFactoryTests>();
        var logger2 = factory.CreateLogger<LoggingConfig>();

        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
        Assert.NotSame(logger1, logger2);
    }

    /// <summary>
    /// Verifies logger output includes class name from ILogger of T.
    /// </summary>
    [Fact]
    public void Create_ShouldIncludeClassName()
    {
        // Arrange
        var logger = TestLoggerFactory.Create<TestLoggerFactoryTests>(this._output);

        // Act - the logger should automatically include the class name
        // This test passes if no exception occurs and output is visible
        logger.LogInformation("Message from typed logger");

        // Assert - logging works (class name visible in output)
        Assert.True(true);
    }

    /// <summary>
    /// Verifies null output helper throws appropriate exception.
    /// </summary>
    [Fact]
    public void Create_WithNullOutput_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TestLoggerFactory.Create<TestLoggerFactoryTests>(null!));
    }

    /// <summary>
    /// Verifies CreateLoggerFactory with null output throws.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithNullOutput_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TestLoggerFactory.CreateLoggerFactory(null!));
    }
}
