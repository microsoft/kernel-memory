// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for DeleteCommandSettings validation.
/// </summary>
public sealed class DeleteCommandSettingsTests
{
    [Fact]
    public void Validate_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var settings = new DeleteCommandSettings
        {
            Id = "delete-id-456"
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
        var settings = new DeleteCommandSettings
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
        var settings = new DeleteCommandSettings
        {
            Id = "  \t  "
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
        var settings = new DeleteCommandSettings
        {
            Id = "valid-id",
            Verbosity = "invalid-verbosity"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }
}
