// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI;
using KernelMemory.Main.CLI.OutputFormatters;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.OutputFormatters;

/// <summary>
/// Unit tests for OutputFormatterFactory.
/// </summary>
public sealed class OutputFormatterFactoryTests
{
    [Fact]
    public void Create_WithJsonFormat_ReturnsJsonFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "json",
            Verbosity = "normal"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<JsonOutputFormatter>(formatter);
        Assert.Equal("normal", formatter.Verbosity);
    }

    [Fact]
    public void Create_WithYamlFormat_ReturnsYamlFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "yaml",
            Verbosity = "quiet"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<YamlOutputFormatter>(formatter);
        Assert.Equal("quiet", formatter.Verbosity);
    }

    [Fact]
    public void Create_WithHumanFormat_ReturnsHumanFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "human",
            Verbosity = "verbose"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<HumanOutputFormatter>(formatter);
        Assert.Equal("verbose", formatter.Verbosity);
    }

    [Fact]
    public void Create_WithUpperCaseFormat_HandlesCorrectly()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "JSON",
            Verbosity = "normal"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<JsonOutputFormatter>(formatter);
    }

    [Fact]
    public void Create_WithMixedCaseFormat_HandlesCorrectly()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "YaML",
            Verbosity = "normal"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<YamlOutputFormatter>(formatter);
    }

    [Fact]
    public void Create_WithDefaultFormat_ReturnsHumanFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "unknown-format",
            Verbosity = "normal"
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<HumanOutputFormatter>(formatter);
    }

    [Fact]
    public void Create_WithNoColor_PassesToHumanFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "human",
            Verbosity = "normal",
            NoColor = true
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<HumanOutputFormatter>(formatter);
    }

    [Fact]
    public void Create_WithColors_PassesToHumanFormatter()
    {
        // Arrange
        var settings = new GlobalOptions
        {
            Format = "human",
            Verbosity = "normal",
            NoColor = false
        };

        // Act
        var formatter = OutputFormatterFactory.Create(settings);

        // Assert
        Assert.IsType<HumanOutputFormatter>(formatter);
    }
}
