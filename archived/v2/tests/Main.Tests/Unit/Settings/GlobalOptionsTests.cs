// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI;

namespace KernelMemory.Main.Tests.Unit.Settings;

/// <summary>
/// Unit tests for GlobalOptions validation.
/// </summary>
public sealed class GlobalOptionsTests
{
    [Fact]
    public void Validate_WithValidHumanFormat_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Format = "human" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithValidJsonFormat_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Format = "json" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithValidYamlFormat_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Format = "yaml" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithUpperCaseFormat_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Format = "JSON" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidFormat_ReturnsError()
    {
        // Arrange
        var options = new GlobalOptions { Format = "xml" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("Format must be", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithValidSilentVerbosity_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "silent" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithValidQuietVerbosity_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "quiet" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithValidNormalVerbosity_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "normal" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithValidVerboseVerbosity_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "verbose" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithUpperCaseVerbosity_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "VERBOSE" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void Validate_WithInvalidVerbosity_ReturnsError()
    {
        // Arrange
        var options = new GlobalOptions { Verbosity = "debug" };

        // Act
        var result = options.Validate();

        // Assert
        Assert.False(result.Successful);
        Assert.Contains("Verbosity must be", result.Message ?? string.Empty);
    }

    [Fact]
    public void Validate_WithAllValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new GlobalOptions
        {
            Format = "json",
            Verbosity = "quiet",
            ConfigPath = "/path/to/config.json",
            NodeName = "my-node",
            NoColor = true
        };

        // Act
        var result = options.Validate();

        // Assert
        Assert.True(result.Successful);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new GlobalOptions();

        // Assert
        Assert.Equal("human", options.Format);
        Assert.Equal("normal", options.Verbosity);
        Assert.False(options.NoColor);
        Assert.Null(options.ConfigPath);
        Assert.Null(options.NodeName);
    }
}
