// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI.Commands;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for NodesCommandSettings.
/// </summary>
public sealed class NodesCommandSettingsTests
{
    [Fact]
    public void Validate_WithDefaultOptions_ReturnsSuccess()
    {
        // Arrange
        var settings = new NodesCommandSettings();

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithJsonFormat_ReturnsSuccess()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            Format = "json"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithYamlFormat_ReturnsSuccess()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            Format = "yaml"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            Format = "xml"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidVerbosity_ReturnsError()
    {
        // Arrange
        var settings = new NodesCommandSettings
        {
            Verbosity = "trace"
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.Successful);
    }
}
