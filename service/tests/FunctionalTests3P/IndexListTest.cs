// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests3P.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests3P;

public class IndexListTest : BaseTestCase
{
    public IndexListTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("simple_volatile")]
    [InlineData("az_ai_search")]
    public async Task ItListsIndexes(string memoryType)
    {
        // Arrange
        var memory = this.GetServerlessMemory(memoryType);
        string indexName1 = Guid.NewGuid().ToString("D");
        string indexName2 = Guid.NewGuid().ToString("D");
        Console.WriteLine("Index 1:" + indexName1);
        Console.WriteLine("Index 2:" + indexName2);
        await memory.ImportTextAsync("text1", index: indexName1, steps: Constants.PipelineWithoutSummary);
        await memory.ImportTextAsync("text2", index: indexName2, steps: Constants.PipelineWithoutSummary);

        // Act
        List<IndexDetails> list = (await memory.ListIndexesAsync()).ToList();
        Console.WriteLine("Indexes found:");
        foreach (var index in list)
        {
            Console.WriteLine(" - " + index.Name);
        }

        // Clean up before exceptions can occur
        await memory.DeleteIndexAsync(indexName1);
        await memory.DeleteIndexAsync(indexName2);

        // Assert
        Assert.True(list.Any(x => x.Name == indexName1));
        Assert.True(list.Any(x => x.Name == indexName2));
    }
}
