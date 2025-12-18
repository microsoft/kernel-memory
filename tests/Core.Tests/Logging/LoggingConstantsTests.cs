// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for LoggingConstants - validates that all logging magic values are defined
/// and have appropriate values for the application.
/// </summary>
public sealed class LoggingConstantsTests
{
    /// <summary>
    /// Verifies default log file size limit is reasonable (100MB).
    /// The limit prevents excessive disk usage while allowing enough history.
    /// </summary>
    [Fact]
    public void DefaultFileSizeLimitBytes_ShouldBe100MB()
    {
        // Arrange & Act
        const long expectedBytes = 100 * 1024 * 1024;

        // Assert
        Assert.Equal(expectedBytes, Constants.LoggingDefaults.DefaultFileSizeLimitBytes);
    }

    /// <summary>
    /// Verifies retained file count is 30 (approximately 1 month of daily logs).
    /// This balances disk usage with diagnostic history requirements.
    /// </summary>
    [Fact]
    public void DefaultRetainedFileCountLimit_ShouldBe30()
    {
        // Assert
        Assert.Equal(30, Constants.LoggingDefaults.DefaultRetainedFileCountLimit);
    }

    /// <summary>
    /// Verifies default log level for file output is Information.
    /// This provides useful diagnostics without excessive verbosity.
    /// </summary>
    [Fact]
    public void DefaultFileLogLevel_ShouldBeInformation()
    {
        // Assert
        Assert.Equal(Serilog.Events.LogEventLevel.Information, Constants.LoggingDefaults.DefaultFileLogLevel);
    }

    /// <summary>
    /// Verifies default log level for console stderr is Warning.
    /// Only warnings and errors should appear on stderr by default.
    /// </summary>
    [Fact]
    public void DefaultConsoleLogLevel_ShouldBeWarning()
    {
        // Assert
        Assert.Equal(Serilog.Events.LogEventLevel.Warning, Constants.LoggingDefaults.DefaultConsoleLogLevel);
    }

    /// <summary>
    /// Verifies environment variable name for .NET environment detection.
    /// </summary>
    [Fact]
    public void DotNetEnvironmentVariable_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("DOTNET_ENVIRONMENT", Constants.LoggingDefaults.DotNetEnvironmentVariable);
    }

    /// <summary>
    /// Verifies fallback environment variable name for ASP.NET Core.
    /// </summary>
    [Fact]
    public void AspNetCoreEnvironmentVariable_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("ASPNETCORE_ENVIRONMENT", Constants.LoggingDefaults.AspNetCoreEnvironmentVariable);
    }

    /// <summary>
    /// Verifies default environment is Development when none is set.
    /// </summary>
    [Fact]
    public void DefaultEnvironment_ShouldBeDevelopment()
    {
        // Assert
        Assert.Equal("Development", Constants.LoggingDefaults.DefaultEnvironment);
    }

    /// <summary>
    /// Verifies Production environment name constant.
    /// </summary>
    [Fact]
    public void ProductionEnvironment_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("Production", Constants.LoggingDefaults.ProductionEnvironment);
    }

    /// <summary>
    /// Verifies the redacted placeholder text for sensitive data scrubbing.
    /// </summary>
    [Fact]
    public void RedactedPlaceholder_ShouldBeDefined()
    {
        // Assert
        Assert.Equal("[REDACTED]", Constants.LoggingDefaults.RedactedPlaceholder);
    }

    /// <summary>
    /// Verifies the output template for human-readable logs.
    /// </summary>
    [Fact]
    public void HumanReadableOutputTemplate_ShouldContainTimestampAndLevel()
    {
        // Arrange & Act
        const string template = Constants.LoggingDefaults.HumanReadableOutputTemplate;

        // Assert - template should contain key elements
        Assert.Contains("{Timestamp", template);
        Assert.Contains("{Level", template);
        Assert.Contains("{Message", template);
    }
}
