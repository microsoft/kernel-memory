// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Logging;
using Serilog.Core;
using Serilog.Events;

namespace KernelMemory.Core.Tests.Logging;

/// <summary>
/// Tests for SensitiveDataScrubbingPolicy - validates sensitive data protection.
/// In Production environment, all string parameters must be scrubbed to prevent data leakage.
/// In Development/Staging, full logging is allowed for debugging.
/// </summary>
public sealed class SensitiveDataScrubbingPolicyTests : IDisposable
{
    private readonly string? _originalDotNetEnv;
    private readonly SensitiveDataScrubbingPolicy _policy;

    /// <summary>
    /// Initializes test capturing original environment.
    /// </summary>
    public SensitiveDataScrubbingPolicyTests()
    {
        this._originalDotNetEnv = Environment.GetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable);
        this._policy = new SensitiveDataScrubbingPolicy();
    }

    /// <summary>
    /// Restores original environment after test.
    /// </summary>
    public void Dispose()
    {
        if (this._originalDotNetEnv != null)
        {
            Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, this._originalDotNetEnv);
        }
        else
        {
            Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies strings are scrubbed in Production environment.
    /// This is critical for preventing sensitive data in production logs.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndString_ShouldScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        const string sensitiveValue = "secret-api-key-12345";

        // Act
        var handled = this._policy.TryDestructure(sensitiveValue, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.True(handled);
        Assert.NotNull(result);
        Assert.IsType<ScalarValue>(result);
        Assert.Equal(LoggingConstants.RedactedPlaceholder, ((ScalarValue)result).Value);
    }

    /// <summary>
    /// Verifies strings are NOT scrubbed in Development environment.
    /// Development needs full logging for debugging.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenDevelopmentAndString_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Development");
        const string value = "test-value";

        // Act
        var handled = this._policy.TryDestructure(value, new TestPropertyValueFactory(), out var result);

        // Assert - not handled means Serilog will process normally
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies strings are NOT scrubbed when no environment is set.
    /// Default to Development behavior (full logging).
    /// </summary>
    [Fact]
    public void TryDestructure_WhenNoEnvironmentAndString_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(LoggingConstants.AspNetCoreEnvironmentVariable, null);
        const string value = "test-value";

        // Act
        var handled = this._policy.TryDestructure(value, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies non-string types are NOT scrubbed even in Production.
    /// Only strings are considered potentially sensitive.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndInteger_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");

        // Act
        var handled = this._policy.TryDestructure(42, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies DateTime is NOT scrubbed.
    /// Timestamps are not sensitive data.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndDateTime_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        var dateTime = DateTimeOffset.UtcNow;

        // Act
        var handled = this._policy.TryDestructure(dateTime, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies booleans are NOT scrubbed.
    /// Boolean values are not sensitive.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndBoolean_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");

        // Act
        var handled = this._policy.TryDestructure(true, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies Guids are NOT scrubbed.
    /// IDs are typically logged for correlation and not considered sensitive.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndGuid_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");
        var guid = Guid.NewGuid();

        // Act
        var handled = this._policy.TryDestructure(guid, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies empty strings are still scrubbed in Production.
    /// Even empty strings should be hidden to prevent information leakage.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndEmptyString_ShouldScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");

        // Act
        var handled = this._policy.TryDestructure(string.Empty, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.True(handled);
        Assert.NotNull(result);
        Assert.Equal(LoggingConstants.RedactedPlaceholder, ((ScalarValue)result).Value);
    }

    /// <summary>
    /// Verifies null values are NOT scrubbed.
    /// Null doesn't contain data to protect.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenProductionAndNull_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Production");

        // Act
        var handled = this._policy.TryDestructure(null!, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies strings are NOT scrubbed in Staging environment.
    /// Staging is not Production, so full logging is allowed.
    /// </summary>
    [Fact]
    public void TryDestructure_WhenStagingAndString_ShouldNotScrub()
    {
        // Arrange
        Environment.SetEnvironmentVariable(LoggingConstants.DotNetEnvironmentVariable, "Staging");
        const string value = "test-value";

        // Act
        var handled = this._policy.TryDestructure(value, new TestPropertyValueFactory(), out var result);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    /// <summary>
    /// Test implementation of ILogEventPropertyValueFactory for unit testing.
    /// </summary>
    private sealed class TestPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            return new ScalarValue(value);
        }
    }
}
