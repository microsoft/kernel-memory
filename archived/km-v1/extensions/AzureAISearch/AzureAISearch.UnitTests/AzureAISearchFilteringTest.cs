// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Microsoft.KM.TestHelpers;

namespace Microsoft.AzureAISearch.UnitTests;

public class AzureAISearchFilteringTest : BaseUnitTestCase
{
    public AzureAISearchFilteringTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItRendersEmptyFilters()
    {
        // Arrange
        List<MemoryFilter> filters1 = null!;
        List<MemoryFilter> filters2 = [];

        // Act
        var result1 = AzureAISearchFiltering.BuildSearchFilter(filters1);
        var result2 = AzureAISearchFiltering.BuildSearchFilter(filters2);

        // Assert
        Console.WriteLine($"Result 1: {result1}");
        Console.WriteLine($"Result 2: {result2}");
        Assert.Equal(string.Empty, result1);
        Assert.Equal(string.Empty, result2);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItRendersSimpleFilter()
    {
        // Arrange
        List<MemoryFilter> filters = [MemoryFilters.ByTag("color", "blue")];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItRendersSimpleFilters()
    {
        // Arrange
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue"),
            MemoryFilters.ByTag("size", "medium")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue')) or (tags/any(s: s eq 'size:medium'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItRendersUsingSearchIn()
    {
        // Arrange
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue"),
            MemoryFilters.ByTag("color", "green")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("tags/any(s: search.in(s, 'color:blue|color:green', '|'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItUsesSearchInWithAlternativeSeparators()
    {
        // Arrange
        List<MemoryFilter> filters1 =
        [
            MemoryFilters.ByTag("color", "blue"),
            MemoryFilters.ByTag("color", "green")
        ];
        List<MemoryFilter> filters2 =
        [
            MemoryFilters.ByTag("color", "bl|ue"),
            MemoryFilters.ByTag("color", "gr|e|en")
        ];

        // Act
        var result1 = AzureAISearchFiltering.BuildSearchFilter(filters1);
        var result2 = AzureAISearchFiltering.BuildSearchFilter(filters2);

        // Assert
        Console.WriteLine($"Result 1: {result1}");
        Console.WriteLine($"Result 2: {result2}");
        Assert.Equal("tags/any(s: search.in(s, 'color:blue|color:green', '|'))", result1);
        Assert.Equal("tags/any(s: search.in(s, 'color:bl|ue,color:gr|e|en', ','))", result2);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesComplexFilters()
    {
        // Arrange
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue").ByTag("size", "medium"),
            MemoryFilters.ByTag("color", "blue")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue') and tags/any(s: s eq 'size:medium')) or " +
                     "(tags/any(s: s eq 'color:blue'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase0()
    {
        // Arrange
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("col|or", "blue"),
            MemoryFilters.ByTag("si,ze", "small")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'col|or:blue')) or (tags/any(s: s eq 'si,ze:small'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase1()
    {
        // Arrange - (col|or == blue) OR (col|or == white) OR (si,ze == small) OR (si,ze == medium)
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("col|or", "blue"),
            MemoryFilters.ByTag("col|or", "white"),
            MemoryFilters.ByTag("si,ze", "small"),
            MemoryFilters.ByTag("si,ze", "medium")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("tags/any(s: search.in(s, 'col|or:blue,col|or:white', ',')) or " +
                     "tags/any(s: search.in(s, 'si,ze:small|si,ze:medium', '|'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase2()
    {
        // Arrange - (col|or == blue) OR (col|or == white) OR (si,ze == small) OR (si,ze == medium)
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("col|or", "blue"),
            MemoryFilters.ByTag("col|or", "white"),
            MemoryFilters.ByTag("si,ze", "sm|all"),
            MemoryFilters.ByTag("si,ze", "med;ium")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("tags/any(s: search.in(s, 'col|or:blue,col|or:white', ',')) or " +
                     "tags/any(s: search.in(s, 'si,ze:sm|all-si,ze:med;ium', '-'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase3()
    {
        // Arrange - color == blue AND color == blue
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue").ByTag("color", "blue")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue') and tags/any(s: s eq 'color:blue'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase4()
    {
        // Arrange - (color == blue) OR (color == blue)
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue"),
            MemoryFilters.ByTag("color", "blue")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("tags/any(s: search.in(s, 'color:blue|color:blue', '|'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase5()
    {
        // Arrange - color == blue AND color == green
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue").ByTag("color", "green")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue') and tags/any(s: s eq 'color:green'))", result);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    [Trait("Category", "AzAISearch")]
    public void ItHandlesEdgeCase6()
    {
        // Arrange - (color == blue AND color == green) OR (color == green)
        List<MemoryFilter> filters =
        [
            MemoryFilters.ByTag("color", "blue").ByTag("color", "green"),
            MemoryFilters.ByTag("color", "green")
        ];

        // Act
        var result = AzureAISearchFiltering.BuildSearchFilter(filters);

        // Assert
        Console.WriteLine($"Result: {result}");
        Assert.Equal("(tags/any(s: s eq 'color:blue') and tags/any(s: s eq 'color:green')) " +
                     "or (tags/any(s: s eq 'color:green'))", result);
    }
}
