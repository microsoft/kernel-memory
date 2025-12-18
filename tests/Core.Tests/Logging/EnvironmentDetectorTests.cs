// Copyright (c) Microsoft. All rights reserved.

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for EnvironmentDetector - validates environment detection logic.
/// Environment detection is critical for security (sensitive data scrubbing).
/// </summary>
[Collection("EnvironmentVariables")]
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
        this._originalDotNetEnv = Environment.GetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable);
        this._originalAspNetEnv = Environment.GetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable);
    }

    /// <summary>
    /// Restores original environment variables after test.
    /// </summary>
    public void Dispose()
    {
        // Restore original environment variables
        if (this._originalDotNetEnv != null)
        {
            Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, this._originalDotNetEnv);
        }
        else
        {
            Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, null);
        }

        if (this._originalAspNetEnv != null)
        {
            Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, this._originalAspNetEnv);
        }
        else
        {
            Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);
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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, "Staging");

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, "Staging");

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "production");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "PRODUCTION");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        // Arrange - set DOTNET_ENVIRONMENT to Development (takes precedence over ASPNETCORE_ENVIRONMENT)
        // Set both to Development to ensure no Production leaks from other tests
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Development");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, string.Empty);

        // Act
        var result = EnvironmentDetector.IsProduction();

        // Assert - Development environment should not be Production
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsProduction returns false for Staging.
    /// </summary>
    [Fact]
    public void IsProduction_WhenStaging_ShouldReturnFalse()
    {
        // Arrange - clear both env vars to ensure isolation
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Staging");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Development");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "Production");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, null);

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, string.Empty);
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, "Staging");

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
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.DotNetEnvironmentVariable, "   ");
        Environment.SetEnvironmentVariable(Constants.LoggingDefaults.AspNetCoreEnvironmentVariable, "Staging");

        // Act
        var result = EnvironmentDetector.GetEnvironment();

        // Assert
        Assert.Equal("Staging", result);
    }
}
