// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Elasticsearch.FunctionalTests.Additional;

public class IndexManagementTests : MemoryDbFunctionalTest
{
    public IndexManagementTests(
        IConfiguration cfg,
        ITestOutputHelper output)
        : base(cfg, output)
    {
    }

    [Fact]
    public async Task CanCreateAndDeleteIndexAsync()
    {
        var indexName = nameof(this.CanCreateAndDeleteIndexAsync);
        var vectorSize = 1536;

        // Creates the index using IMemoryDb
        await this.MemoryDb.CreateIndexAsync(indexName, vectorSize)
            .ConfigureAwait(false);

        // Verifies the index is created using the ES client
        var actualIndexName = IndexNameHelper.Convert(nameof(this.CanCreateAndDeleteIndexAsync), base.ElasticsearchConfig);
        var resp = await this.Client.Indices.ExistsAsync(actualIndexName)
            .ConfigureAwait(false);
        Assert.True(resp.Exists);
        this.Output.WriteLine($"The index '{actualIndexName}' was created successfully.");

        // Deletes the index
        await this.MemoryDb.DeleteIndexAsync(indexName)
            .ConfigureAwait(false);

        // Verifies the index is deleted using the ES client
        resp = await this.Client.Indices.ExistsAsync(actualIndexName)
            .ConfigureAwait(false);
        Assert.False(resp.Exists);
        this.Output.WriteLine($"The index '{actualIndexName}' was deleted successfully.");
    }

    [Fact]
    public async Task CanGetIndicesAsync()
    {
        var indexNames = new[]
        {
            IndexNameHelper.Convert(nameof(this.CanGetIndicesAsync) + "-First", base.ElasticsearchConfig),
            IndexNameHelper.Convert(nameof(this.CanGetIndicesAsync) + "-Second", base.ElasticsearchConfig)
        };

        // Creates the indices using IMemoryDb
        foreach (var indexName in indexNames)
        {
            await this.MemoryDb.CreateIndexAsync(indexName, 1536)
                .ConfigureAwait(false);
        }

        // Verifies the indices are returned
        var indices = await this.MemoryDb.GetIndexesAsync()
            .ConfigureAwait(false);

        Assert.True(indices.All(nme => indices.Contains(nme)));

        // Cleans up
        foreach (var indexName in indexNames)
        {
            await this.MemoryDb.DeleteIndexAsync(indexName)
                .ConfigureAwait(false);
        }
    }
}
