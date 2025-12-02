// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Logging;
using Serilog.Events;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for LoggingConfig - validates logging configuration model behavior.
/// LoggingConfig stores level and file path settings for the logging system.
/// </summary>
public sealed class LoggingConfigTests
{
    /// <summary>
    /// Verifies default log level is Information when not specified.
    /// Information level provides good diagnostics without excessive verbosity.
    /// </summary>
    [Fact]
    public void DefaultLevel_ShouldBeInformation()
    {
        // Arrange & Act
        var config = new LoggingConfig();

        // Assert
        Assert.Equal(LogEventLevel.Information, config.Level);
    }

    /// <summary>
    /// Verifies file path is null by default (file logging disabled).
    /// File logging should only be enabled when explicitly configured.
    /// </summary>
    [Fact]
    public void DefaultFilePath_ShouldBeNull()
    {
        // Arrange & Act
        var config = new LoggingConfig();

        // Assert
        Assert.Null(config.FilePath);
    }

    /// <summary>
    /// Verifies log level can be set to Verbose for detailed debugging.
    /// </summary>
    [Fact]
    public void Level_CanBeSetToVerbose()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.Level = LogEventLevel.Verbose;

        // Assert
        Assert.Equal(LogEventLevel.Verbose, config.Level);
    }

    /// <summary>
    /// Verifies log level can be set to Debug.
    /// </summary>
    [Fact]
    public void Level_CanBeSetToDebug()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.Level = LogEventLevel.Debug;

        // Assert
        Assert.Equal(LogEventLevel.Debug, config.Level);
    }

    /// <summary>
    /// Verifies log level can be set to Warning.
    /// </summary>
    [Fact]
    public void Level_CanBeSetToWarning()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.Level = LogEventLevel.Warning;

        // Assert
        Assert.Equal(LogEventLevel.Warning, config.Level);
    }

    /// <summary>
    /// Verifies log level can be set to Error.
    /// </summary>
    [Fact]
    public void Level_CanBeSetToError()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.Level = LogEventLevel.Error;

        // Assert
        Assert.Equal(LogEventLevel.Error, config.Level);
    }

    /// <summary>
    /// Verifies log level can be set to Fatal.
    /// </summary>
    [Fact]
    public void Level_CanBeSetToFatal()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.Level = LogEventLevel.Fatal;

        // Assert
        Assert.Equal(LogEventLevel.Fatal, config.Level);
    }

    /// <summary>
    /// Verifies file path can be set to a relative path.
    /// Relative paths are resolved relative to current working directory.
    /// </summary>
    [Fact]
    public void FilePath_CanBeSetToRelativePath()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.FilePath = "logs/app.log";

        // Assert
        Assert.Equal("logs/app.log", config.FilePath);
    }

    /// <summary>
    /// Verifies file path can be set to an absolute path.
    /// </summary>
    [Fact]
    public void FilePath_CanBeSetToAbsolutePath()
    {
        // Arrange
        var config = new LoggingConfig();
        var absolutePath = Path.Combine(Path.GetTempPath(), "km", "app.log");

        // Act
        config.FilePath = absolutePath;

        // Assert
        Assert.Equal(absolutePath, config.FilePath);
    }

    /// <summary>
    /// Verifies UseJsonFormat is false by default (human-readable).
    /// Human-readable format is better for development and CLI usage.
    /// </summary>
    [Fact]
    public void DefaultUseJsonFormat_ShouldBeFalse()
    {
        // Arrange & Act
        var config = new LoggingConfig();

        // Assert
        Assert.False(config.UseJsonFormat);
    }

    /// <summary>
    /// Verifies UseJsonFormat can be enabled for structured logging.
    /// </summary>
    [Fact]
    public void UseJsonFormat_CanBeEnabled()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.UseJsonFormat = true;

        // Assert
        Assert.True(config.UseJsonFormat);
    }

    /// <summary>
    /// Verifies UseAsyncLogging is false by default (sync for CLI).
    /// Sync logging is better for short-lived CLI commands.
    /// </summary>
    [Fact]
    public void DefaultUseAsyncLogging_ShouldBeFalse()
    {
        // Arrange & Act
        var config = new LoggingConfig();

        // Assert
        Assert.False(config.UseAsyncLogging);
    }

    /// <summary>
    /// Verifies UseAsyncLogging can be enabled for services.
    /// Async logging improves performance for long-running services.
    /// </summary>
    [Fact]
    public void UseAsyncLogging_CanBeEnabled()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        config.UseAsyncLogging = true;

        // Assert
        Assert.True(config.UseAsyncLogging);
    }

    /// <summary>
    /// Verifies IsFileLoggingEnabled returns false when FilePath is null.
    /// </summary>
    [Fact]
    public void IsFileLoggingEnabled_WhenFilePathIsNull_ShouldReturnFalse()
    {
        // Arrange
        var config = new LoggingConfig { FilePath = null };

        // Act & Assert
        Assert.False(config.IsFileLoggingEnabled);
    }

    /// <summary>
    /// Verifies IsFileLoggingEnabled returns false when FilePath is empty.
    /// </summary>
    [Fact]
    public void IsFileLoggingEnabled_WhenFilePathIsEmpty_ShouldReturnFalse()
    {
        // Arrange
        var config = new LoggingConfig { FilePath = string.Empty };

        // Act & Assert
        Assert.False(config.IsFileLoggingEnabled);
    }

    /// <summary>
    /// Verifies IsFileLoggingEnabled returns false when FilePath is whitespace.
    /// </summary>
    [Fact]
    public void IsFileLoggingEnabled_WhenFilePathIsWhitespace_ShouldReturnFalse()
    {
        // Arrange
        var config = new LoggingConfig { FilePath = "   " };

        // Act & Assert
        Assert.False(config.IsFileLoggingEnabled);
    }

    /// <summary>
    /// Verifies IsFileLoggingEnabled returns true when FilePath is set.
    /// </summary>
    [Fact]
    public void IsFileLoggingEnabled_WhenFilePathIsSet_ShouldReturnTrue()
    {
        // Arrange
        var config = new LoggingConfig { FilePath = "logs/app.log" };

        // Act & Assert
        Assert.True(config.IsFileLoggingEnabled);
    }
}
