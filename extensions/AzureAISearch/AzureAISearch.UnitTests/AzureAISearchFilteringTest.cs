// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;

namespace AzureAISearch.UnitTests;

public class AzureAISearchFilteringTest
{
    [Fact]
    public void ItRendersEmptyFilters()
    {
        // Arrange
        List<MemoryFilter> filters1 = null;
        List<MemoryFilter> filters2 = new();

        // Act
        var result1 = AzureAISearchFiltering.BuildSearchFilter(filters1);
        var result2 = AzureAISearchFiltering.BuildSearchFilter(filters2);

        // Assert
        Assert.Equal(string.Empty, result1);
        Assert.Equal(string.Empty, result2);
    }
}
