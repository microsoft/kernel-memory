// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for ListCommandSettings validation.
/// </summary>
public sealed class ListCommandSettingsTests
{
    [Fact]
    public void Validate_WithValidPagination_ReturnsSuccess()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 0,
            Take = 20
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithNegativeSkip_ReturnsError()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = -1,
            Take = 10
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("Skip must be >= 0", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithZeroTake_ReturnsError()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 0,
            Take = 0
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("Take must be > 0", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithNegativeTake_ReturnsError()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 0,
            Take = -5
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WithLargeSkipValue_ReturnsSuccess()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 1000,
            Take = 10
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithLargeTakeValue_ReturnsSuccess()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 0,
            Take = 100
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidBaseOptions_ReturnsError()
    {
        // Arrange
        var settings = new ListCommandSettings
        {
            Skip = 0,
            Take = 10,
            Format = "invalid"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var settings = new ListCommandSettings();

        // Assert
        Assert.Equal(0, settings.Skip);
        Assert.Equal(Constants.DefaultPageSize, settings.Take);
    }
}
