// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for SerilogFactory - validates logger creation with various configurations.
/// SerilogFactory creates properly configured Serilog loggers with Microsoft.Extensions.Logging integration.
/// </summary>
public sealed class SerilogFactoryTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Initializes test with temp directory for file logging tests.
    /// </summary>
    public SerilogFactoryTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(this._tempDir);
    }

    /// <summary>
    /// Cleans up temp directory after tests.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Cleanup errors should not fail tests")]
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors in tests - files may be locked
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies CreateLoggerFactory returns non-null factory.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithDefaultConfig_ShouldReturnFactory()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);

        // Assert
        Assert.NotNull(factory);
    }

    /// <summary>
    /// Verifies factory can create typed loggers.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_ShouldCreateTypedLogger()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();

        // Assert
        Assert.NotNull(logger);
    }

    /// <summary>
    /// Verifies logger respects minimum level from config.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithWarningLevel_ShouldFilterDebugLogs()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Level = LogEventLevel.Warning
        };

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();

        // Assert - Debug should be disabled when minimum level is Warning
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    /// <summary>
    /// Verifies logger enables all levels when set to Verbose.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithVerboseLevel_ShouldEnableAllLevels()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Level = LogEventLevel.Verbose
        };

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();

        // Assert
        Assert.True(logger.IsEnabled(LogLevel.Trace));
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    /// <summary>
    /// Verifies file logging creates log file when configured.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithFilePath_ShouldCreateLogFile()
    {
        // Arrange
        var logPath = Path.Combine(this._tempDir, "test.log");
        var config = new LoggingConfig
        {
            FilePath = logPath
        };

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();
        logger.LogInformation("Test message for file creation");

        // Force flush by disposing
        factory.Dispose();

        // Assert - file should exist (Serilog may add date suffix)
        var logFiles = Directory.GetFiles(this._tempDir, "test*.log");
        Assert.NotEmpty(logFiles);
    }

    /// <summary>
    /// Verifies file logging writes messages to file.
    /// </summary>
    [Fact]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Test validates unique message content")]
    public void CreateLoggerFactory_WithFilePath_ShouldWriteToFile()
    {
        // Arrange
        var logPath = Path.Combine(this._tempDir, "messages.log");
        var config = new LoggingConfig
        {
            FilePath = logPath,
            Level = LogEventLevel.Information
        };
        var testMessage = $"Test message {Guid.NewGuid()}";

        // Act
        using (var factory = SerilogFactory.CreateLoggerFactory(config))
        {
            var logger = factory.CreateLogger<SerilogFactoryTests>();
            logger.LogInformation(testMessage);
        }

        // Assert
        var logFiles = Directory.GetFiles(this._tempDir, "messages*.log");
        Assert.NotEmpty(logFiles);

        var content = File.ReadAllText(logFiles[0]);
        Assert.Contains(testMessage, content);
    }

    /// <summary>
    /// Verifies JSON format writes structured log entries.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithJsonFormat_ShouldWriteJsonToFile()
    {
        // Arrange
        var logPath = Path.Combine(this._tempDir, "json.log");
        var config = new LoggingConfig
        {
            FilePath = logPath,
            UseJsonFormat = true,
            Level = LogEventLevel.Information
        };

        // Act
        using (var factory = SerilogFactory.CreateLoggerFactory(config))
        {
            var logger = factory.CreateLogger<SerilogFactoryTests>();
            logger.LogInformation("JSON test message");
        }

        // Assert
        var logFiles = Directory.GetFiles(this._tempDir, "json*.log");
        Assert.NotEmpty(logFiles);

        var content = File.ReadAllText(logFiles[0]);
        // JSON format should contain these markers
        Assert.Contains("{", content);
        Assert.Contains("\"", content);
    }

    /// <summary>
    /// Verifies logger factory can be disposed multiple times without error.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_DisposeTwice_ShouldNotThrow()
    {
        // Arrange
        var config = new LoggingConfig();
        var factory = SerilogFactory.CreateLoggerFactory(config);

        // Act
        factory.Dispose();
        var exception = Record.Exception(() => factory.Dispose());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies CreateLogger extension method creates typed logger.
    /// </summary>
    [Fact]
    public void CreateLogger_WithType_ShouldReturnTypedLogger()
    {
        // Arrange
        var config = new LoggingConfig();

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();

        // Assert
        Assert.NotNull(logger);
    }

    /// <summary>
    /// Verifies file logging creates directory if it doesn't exist.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithNestedFilePath_ShouldCreateDirectory()
    {
        // Arrange
        var logPath = Path.Combine(this._tempDir, "nested", "dir", "test.log");
        var config = new LoggingConfig
        {
            FilePath = logPath
        };

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();
        logger.LogInformation("Test");

        // Force flush
        factory.Dispose();

        // Assert - nested directory should be created
        Assert.True(Directory.Exists(Path.GetDirectoryName(logPath)));
    }

    /// <summary>
    /// Verifies async logging option is respected.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithAsyncLogging_ShouldNotThrow()
    {
        // Arrange
        var logPath = Path.Combine(this._tempDir, "async.log");
        var config = new LoggingConfig
        {
            FilePath = logPath,
            UseAsyncLogging = true
        };

        // Act
        var exception = Record.Exception(() =>
        {
            using var factory = SerilogFactory.CreateLoggerFactory(config);
            var logger = factory.CreateLogger<SerilogFactoryTests>();
            logger.LogInformation("Async test");
        });

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies error level config filters out lower levels.
    /// </summary>
    [Fact]
    public void CreateLoggerFactory_WithErrorLevel_ShouldFilterLowerLevels()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Level = LogEventLevel.Error
        };

        // Act
        using var factory = SerilogFactory.CreateLoggerFactory(config);
        var logger = factory.CreateLogger<SerilogFactoryTests>();

        // Assert
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }
}
