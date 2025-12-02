// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Logging;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for EnvironmentDetector - validates environment detection logic.
/// Environment detection is critical for security (sensitive data scrubbing).
/// </summary>
public sealed class EnvironmentDetectorTests : IDisposable
{
    private readonly string? _originalDotNetEnv;
    private readonly string? _originalAspNetEnv;

    /// <summary>
    /// Initializes a new instance capturing original environment variables.
    /// </summary>
    public EnvironmentDetectorTests()
    {
        // Capture original values to restore after tests
        this._originalDotNetEnv = Environment.GetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable);
        this._originalAspNetEnv = Environment.GetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable);
    }

    /// <summary>
    /// Restores original environment variables after test.
    /// </summary>
    public void Dispose()
    {
        // Restore original environment variables
        if (this._originalDotNetEnv != null)
        {
            Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, this._originalDotNetEnv);
        }
        else
        {
            Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        }

        if (this._originalAspNetEnv != null)
        {
            Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, this._originalAspNetEnv);
        }
        else
        {
            Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies GetEnvironment returns DOTNET_ENVIRONMENT when set.
    /// DOTNET_ENVIRONMENT takes precedence over all other sources.
    /// </summary>
    [Fact]
    public void GetEnvironment_WhenDotNetEnvSet_ShouldReturnDotNetEnv()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, "Staging");

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert - DOTNET_ENVIRONMENT takes precedence
        Assert.Equal("Production", result);
    }

    /// <summary>
    /// Verifies GetEnvironment falls back to ASPNETCORE_ENVIRONMENT.
    /// When DOTNET_ENVIRONMENT is not set, use ASPNETCORE_ENVIRONMENT.
    /// </summary>
    [Fact]
    public void GetEnvironment_WhenOnlyAspNetEnvSet_ShouldReturnAspNetEnv()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, "Staging");

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert
        Assert.Equal("Staging", result);
    }

    /// <summary>
    /// Verifies GetEnvironment returns Development by default.
    /// When no environment variables are set, default to Development for safety.
    /// </summary>
    [Fact]
    public void GetEnvironment_WhenNothingSet_ShouldReturnDevelopment()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert
        Assert.Equal("Development", result);
    }

    /// <summary>
    /// Verifies IsProduction returns true when Production environment is set.
    /// </summary>
    [Fact]
    public void IsProduction_WhenProductionSet_ShouldReturnTrue()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsProduction is case-insensitive.
    /// Environment values should be compared case-insensitively.
    /// </summary>
    [Fact]
    public void IsProduction_WhenProductionLowercase_ShouldReturnTrue()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "production");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsProduction is case-insensitive (uppercase).
    /// </summary>
    [Fact]
    public void IsProduction_WhenProductionUppercase_ShouldReturnTrue()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "PRODUCTION");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsProduction returns false for Development.
    /// </summary>
    [Fact]
    public void IsProduction_WhenDevelopment_ShouldReturnFalse()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Development");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsProduction returns false for Staging.
    /// </summary>
    [Fact]
    public void IsProduction_WhenStaging_ShouldReturnFalse()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Staging");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsProduction returns false when no environment is set.
    /// Default to Development which is not production.
    /// </summary>
    [Fact]
    public void IsProduction_WhenNotSet_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsDevelopment returns true for Development environment.
    /// </summary>
    [Fact]
    public void IsDevelopment_WhenDevelopmentSet_ShouldReturnTrue()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Development");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsDevelopment();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsDevelopment returns true when no environment is set.
    /// </summary>
    [Fact]
    public void IsDevelopment_WhenNotSet_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsDevelopment();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies IsDevelopment returns false for Production.
    /// </summary>
    [Fact]
    public void IsDevelopment_WhenProduction_ShouldReturnFalse()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);

        // Act
        var result = EnvironmentDetector.IsDevelopment();

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies empty environment variable is treated as not set.
    /// </summary>
    [Fact]
    public void GetEnvironment_WhenDotNetEnvIsEmpty_ShouldFallbackToAspNet()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, string.Empty);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, "Staging");

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert
        Assert.Equal("Staging", result);
    }

    /// <summary>
    /// Verifies whitespace environment variable is treated as not set.
    /// </summary>
    [Fact]
    public void GetEnvironment_WhenDotNetEnvIsWhitespace_ShouldFallbackToAspNet()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "   ");
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, "Staging");

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert
        Assert.Equal("Staging", result);
    }
}
