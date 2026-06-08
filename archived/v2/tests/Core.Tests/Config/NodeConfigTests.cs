// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.ContentIndex;
using KernelMemory.Core.Config.Validation;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests for NodeConfig with Weight property.
/// </summary>
public sealed class NodeConfigTests
{
    [Fact]
    public void DefaultWeight_IsOne()
    {
        // Arrange & Act
        var config = new NodeConfig();

        // Assert
        Assert.Equal(1.0f, config.Weight);
    }

    [Fact]
    public void Weight_CanBeSet()
    {
        // Arrange & Act
        var config = new NodeConfig { Weight = 0.5f };

        // Assert
        Assert.Equal(0.5f, config.Weight);
    }

    [Fact]
    public void Validate_NegativeWeight_Throws()
    {
        // Arrange
        var config = new NodeConfig
        {
            Id = "test",
            Weight = -1.0f,
            ContentIndex = new SqliteContentIndexConfig { Path = "/tmp/test.db" }
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Test"));
        Assert.Contains("Weight", ex.ConfigPath);
        Assert.Contains("non-negative", ex.Message);
    }

    [Fact]
    public void Validate_ZeroWeight_IsValid()
    {
        // Arrange
        var config = new NodeConfig
        {
            Id = "test",
            Weight = 0.0f,
            ContentIndex = new SqliteContentIndexConfig { Path = "/tmp/test.db" }
        };

        // Act & Assert - should not throw
        config.Validate("Test");
    }

    [Fact]
    public void Validate_PositiveWeight_IsValid()
    {
        // Arrange
        var config = new NodeConfig
        {
            Id = "test",
            Weight = 2.0f,
            ContentIndex = new SqliteContentIndexConfig { Path = "/tmp/test.db" }
        };

        // Act & Assert - should not throw
        config.Validate("Test");
    }
}
