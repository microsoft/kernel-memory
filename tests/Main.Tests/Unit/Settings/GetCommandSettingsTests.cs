// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for GetCommandSettings validation.
/// </summary>
public sealed class GetCommandSettingsTests
{
    [Fact]
    public void Validate_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            Id = "test-id-123"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            Id = string.Empty
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("ID cannot be empty", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithWhitespaceId_ReturnsError()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            Id = "   "
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidBaseOptions_ReturnsError()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            Id = "valid-id",
            Format = "invalid"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WithFullFlag_ReturnsSuccess()
    {
        // Arrange
        var settings = new GetCommandSettings
        {
            Id = "test-id",
            ShowFull = true
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void ShowFull_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new GetCommandSettings
        {
            Id = "test-id"
        };

        // Assert
        Assert.False(settings.ShowFull);
    }
}
