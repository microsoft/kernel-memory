// Copyright (c) Microsoft. All rights reserved.

using FunctionalTests.TestHelpers;
using Microsoft.KernelMemory;
using Xunit.Abstractions;

namespace FunctionalTests.Service;

// ReSharper disable InconsistentNaming
public class IndexListTest : BaseTestCase
{
    private readonly IKernelMemory _memory;

    public IndexListTest(
        IConfiguration cfg,
        ITestOutputHelper output) : base(cfg, output)
    {
        var apiKey = this.Configuration.GetSection("ServiceAuthorization").GetValue<string>("AccessKey");
        this._memory = new MemoryWebClient("http://127.0.0.1:9001/", apiKey: apiKey);
    }

    [Fact]
    public async Task ItListsIndexes()
    {
        // Arrange
        string indexName1 = Guid.NewGuid().ToString("D");
        string indexName2 = Guid.NewGuid().ToString("D");
        Console.WriteLine("Index 1:" + indexName1);
        Console.WriteLine("Index 2:" + indexName2);
        string id1 = await this._memory.ImportTextAsync("text1", index: indexName1, steps: Constants.PipelineWithoutSummary);
        string id2 = await this._memory.ImportTextAsync("text2", index: indexName2, steps: Constants.PipelineWithoutSummary);

        while (!await this._memory.IsDocumentReadyAsync(documentId: id1, index: indexName1))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        while (!await this._memory.IsDocumentReadyAsync(documentId: id2, index: indexName2))
        {
            this.Log("Waiting for memory ingestion to complete...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Act
        List<IndexDetails> list = (await this._memory.ListIndexesAsync()).ToList();
        Console.WriteLine("Indexes found:");
        foreach (var index in list)
        {
            Console.WriteLine(" - " + index.Name);
        }

        // Clean up before exceptions can occur
        await this._memory.DeleteIndexAsync(indexName1);
        await this._memory.DeleteIndexAsync(indexName2);

        // Assert
        Assert.True(list.Any(x => x.Name == indexName1));
        Assert.True(list.Any(x => x.Name == indexName2));
    }
}
