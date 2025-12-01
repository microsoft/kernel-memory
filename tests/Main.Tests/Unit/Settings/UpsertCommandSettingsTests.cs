// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for UpsertCommandSettings validation.
/// </summary>
public sealed class UpsertCommandSettingsTests
{
    [Fact]
    public void Validate_WithValidContent_ReturnsSuccess()
    {
        // Arrange
        var settings = new UpsertCommandSettings
        {
            Content = "Test content"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithEmptyContent_ReturnsError()
    {
        // Arrange
        var settings = new UpsertCommandSettings
        {
            Content = string.Empty
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("Content cannot be empty", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithWhitespaceContent_ReturnsError()
    {
        // Arrange
        var settings = new UpsertCommandSettings
        {
            Content = "   "
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
        var settings = new UpsertCommandSettings
        {
            Content = "Valid content",
            Format = "invalid-format"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsSuccess()
    {
        // Arrange
        var settings = new UpsertCommandSettings
        {
            Content = "Test content",
            Id = "custom-id",
            Title = "Test Title",
            Description = "Test Description",
            Tags = "tag1,tag2",
            MimeType = "text/markdown"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void DefaultMimeType_IsTextPlain()
    {
        // Arrange & Act
        var settings = new UpsertCommandSettings
        {
            Content = "Test"
        };

        // Assert
        Assert.Equal("text/plain", settings.MimeType);
    }
}
