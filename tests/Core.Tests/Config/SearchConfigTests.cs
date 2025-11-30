// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config;
using KernelMemory.Core.Config.Validation;
using KernelMemory.Core.Search;
using Xunit;

namespace KernelMemory.Core.Tests.Config;

/// <summary>
/// Tests for SearchConfig validation and behavior.
/// </summary>
public sealed class SearchConfigTests
{
    [Fact]
    public void DefaultValues_MatchConstants()
    {
        // Arrange & Act
        var config = new SearchConfig();

        // Assert - verify defaults match SearchConstants
        Assert.Equal(SearchConstants.DefaultMinRelevance, config.DefaultMinRelevance);
        Assert.Equal(SearchConstants.DefaultLimit, config.DefaultLimit);
        Assert.Equal(SearchConstants.DefaultSearchTimeoutSeconds, config.SearchTimeoutSeconds);
        Assert.Equal(SearchConstants.DefaultMaxResultsPerNode, config.MaxResultsPerNode);
        Assert.Single(config.DefaultNodes);
        Assert.Equal(SearchConstants.AllNodesWildcard, config.DefaultNodes[0]);
        Assert.Empty(config.ExcludeNodes);
    }

    [Fact]
    public void Validate_ValidConfig_Succeeds()
    {
        // Arrange
        var config = new SearchConfig
        {
            DefaultMinRelevance = 0.5f,
            DefaultLimit = 10,
            SearchTimeoutSeconds = 60,
            MaxResultsPerNode = 500,
            DefaultNodes = ["personal", "work"],
            ExcludeNodes = []
        };

        // Act & Assert - should not throw
        config.Validate("Search");
    }

    [Fact]
    public void Validate_InvalidMinRelevance_Throws()
    {
        // Arrange - below minimum
        var config1 = new SearchConfig { DefaultMinRelevance = -0.1f };

        // Act & Assert
        var ex1 = Assert.Throws<ConfigException>(() => config1.Validate("Search"));
        Assert.Contains("DefaultMinRelevance", ex1.ConfigPath);

        // Arrange - above maximum
        var config2 = new SearchConfig { DefaultMinRelevance = 1.5f };

        // Act & Assert
        var ex2 = Assert.Throws<ConfigException>(() => config2.Validate("Search"));
        Assert.Contains("DefaultMinRelevance", ex2.ConfigPath);
    }

    [Fact]
    public void Validate_InvalidLimit_Throws()
    {
        // Arrange
        var config = new SearchConfig { DefaultLimit = 0 };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("DefaultLimit", ex.ConfigPath);
    }

    [Fact]
    public void Validate_InvalidTimeout_Throws()
    {
        // Arrange
        var config = new SearchConfig { SearchTimeoutSeconds = -1 };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("SearchTimeoutSeconds", ex.ConfigPath);
    }

    [Fact]
    public void Validate_EmptyDefaultNodes_Throws()
    {
        // Arrange
        var config = new SearchConfig { DefaultNodes = [] };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("DefaultNodes", ex.ConfigPath);
        Assert.Contains("at least one node", ex.Message);
    }

    [Fact]
    public void Validate_ContradictoryNodeConfig_Throws()
    {
        // Arrange - same node in both default and exclude
        var config = new SearchConfig
        {
            DefaultNodes = ["personal", "work"],
            ExcludeNodes = ["work", "archive"]
        };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("Contradictory", ex.Message);
        Assert.Contains("work", ex.Message);
    }

    [Fact]
    public void Validate_WildcardWithExclusions_Succeeds()
    {
        // Arrange - wildcard with exclusions is valid
        var config = new SearchConfig
        {
            DefaultNodes = [SearchConstants.AllNodesWildcard],
            ExcludeNodes = ["archive", "temp"]
        };

        // Act & Assert - should not throw
        config.Validate("Search");
    }

    [Fact]
    public void Validate_InvalidQueryComplexityLimits_Throws()
    {
        // Arrange
        var config = new SearchConfig { MaxQueryDepth = 0 };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("MaxQueryDepth", ex.ConfigPath);
    }

    [Fact]
    public void Validate_InvalidSnippetSettings_Throws()
    {
        // Arrange
        var config = new SearchConfig { SnippetLength = -1 };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("SnippetLength", ex.ConfigPath);
    }

    [Fact]
    public void Validate_EmptyHighlightMarkers_Throws()
    {
        // Arrange
        var config = new SearchConfig { HighlightPrefix = "" };

        // Act & Assert
        var ex = Assert.Throws<ConfigException>(() => config.Validate("Search"));
        Assert.Contains("HighlightPrefix", ex.ConfigPath);
    }
}
