// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for LoggerExtensions - validates CallerMemberName-based method logging helpers.
/// These extensions enable automatic method name capture for entry/exit logging.
/// </summary>
public sealed class LoggerExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    /// <summary>
    /// Initializes test with mock logger.
    /// </summary>
    public LoggerExtensionsTests()
    {
        this._mockLogger = new Mock<ILogger>();
        // Enable all log levels for testing
        this._mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    /// <summary>
    /// Verifies LogMethodEntry logs at Debug level.
    /// Method entry should be Debug level to avoid flooding logs.
    /// </summary>
    [Fact]
    public void LogMethodEntry_ShouldLogAtDebugLevel()
    {
        // Arrange & Act
        this._mockLogger.Object.LogMethodEntry();

        // Assert - verify Log was called at Debug level
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies LogMethodEntry captures calling method name automatically.
    /// CallerMemberName attribute should capture the test method name.
    /// </summary>
    [Fact]
    public void LogMethodEntry_ShouldCaptureMethodName()
    {
        // Arrange
        string? capturedState = null;
        this._mockLogger.Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception? ex, Delegate formatter) =>
            {
                capturedState = state?.ToString();
            });

        // Act
        this._mockLogger.Object.LogMethodEntry();

        // Assert - method name should be captured (this test method name)
        Assert.NotNull(capturedState);
        Assert.Contains("LogMethodEntry_ShouldCaptureMethodName", capturedState);
    }

    /// <summary>
    /// Verifies LogMethodEntry includes parameters in log output.
    /// Parameters help with debugging method calls.
    /// </summary>
    [Fact]
    public void LogMethodEntry_WithParameters_ShouldIncludeParametersInLog()
    {
        // Arrange
        string? capturedState = null;
        this._mockLogger.Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception? ex, Delegate formatter) =>
            {
                capturedState = state?.ToString();
            });

        // Act
        this._mockLogger.Object.LogMethodEntry("param1", 42, true);

        // Assert - parameters should be in the log
        Assert.NotNull(capturedState);
        Assert.Contains("param1", capturedState);
    }

    /// <summary>
    /// Verifies LogMethodExit logs at Debug level.
    /// </summary>
    [Fact]
    public void LogMethodExit_ShouldLogAtDebugLevel()
    {
        // Arrange & Act
        this._mockLogger.Object.LogMethodExit();

        // Assert
        this._mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies LogMethodExit captures method name.
    /// </summary>
    [Fact]
    public void LogMethodExit_ShouldCaptureMethodName()
    {
        // Arrange
        string? capturedState = null;
        this._mockLogger.Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception? ex, Delegate formatter) =>
            {
                capturedState = state?.ToString();
            });

        // Act
        this._mockLogger.Object.LogMethodExit();

        // Assert
        Assert.NotNull(capturedState);
        Assert.Contains("LogMethodExit_ShouldCaptureMethodName", capturedState);
    }

    /// <summary>
    /// Verifies LogMethodExit includes result in log output.
    /// Result logging helps with debugging return values.
    /// </summary>
    [Fact]
    public void LogMethodExit_WithResult_ShouldIncludeResultInLog()
    {
        // Arrange
        string? capturedState = null;
        this._mockLogger.Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception? ex, Delegate formatter) =>
            {
                capturedState = state?.ToString();
            });

        // Act
        this._mockLogger.Object.LogMethodExit("success result");

        // Assert
        Assert.NotNull(capturedState);
        Assert.Contains("success result", capturedState);
    }

    /// <summary>
    /// Verifies LogMethodEntry with null parameters handles gracefully.
    /// </summary>
    [Fact]
    public void LogMethodEntry_WithNullParameters_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            this._mockLogger.Object.LogMethodEntry(null, null, null));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies LogMethodExit with null result handles gracefully.
    /// </summary>
    [Fact]
    public void LogMethodExit_WithNullResult_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            this._mockLogger.Object.LogMethodExit(null));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies LogMethodEntry respects log level filtering.
    /// When Debug is disabled, entry logging should be skipped.
    /// </summary>
    [Fact]
    public void LogMethodEntry_WhenDebugDisabled_ShouldNotLog()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);

        // Act
        mockLogger.Object.LogMethodEntry();

        // Assert - Log should not be called when level is disabled
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies LogMethodExit respects log level filtering.
    /// </summary>
    [Fact]
    public void LogMethodExit_WhenDebugDisabled_ShouldNotLog()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);

        // Act
        mockLogger.Object.LogMethodExit();

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
