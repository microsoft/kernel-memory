// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for ConfigCommandSettings.
/// </summary>
public sealed class ConfigCommandSettingsTests
{
    [Fact]
    public void Validate_WithDefaultOptions_ReturnsSuccess()
    {
        // Arrange
        var settings = new ConfigCommandSettings();

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithShowNodes_ReturnsSuccess()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ShowNodes = true
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithShowCache_ReturnsSuccess()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ShowCache = true
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithBothFlags_ReturnsSuccess()
    {
        // Arrange
        var settings = new ConfigCommandSettings
        {
            ShowNodes = true,
            ShowCache = true
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
        var settings = new ConfigCommandSettings
        {
            Format = "xml"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void DefaultFlags_AreFalse()
    {
        // Arrange & Act
        var settings = new ConfigCommandSettings();

        // Assert
        Assert.False(settings.ShowNodes);
        Assert.False(settings.ShowCache);
    }
}
