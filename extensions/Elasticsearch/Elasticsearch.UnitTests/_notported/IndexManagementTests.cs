// Copyright (c) Microsoft. All rights reserved.

using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;
using Microsoft.KernelMemory.MemoryStorage;
using UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Elasticsearch.FunctionalTests._notported;

public class IndexManagementTests : ElasticsearchTestBase
{
    public IndexManagementTests(ITestOutputHelper output, IMemoryDb memoryDb, ElasticsearchClient client, IIndexNameHelper indexNameHelper)
        : base(output, client, indexNameHelper)
    {
        this.MemoryDb = memoryDb ?? throw new ArgumentNullException(nameof(memoryDb));
    }

    public IMemoryDb MemoryDb { get; }

    [Fact]
    public async Task CanCreateAndDeleteIndexAsync()
    {
        var indexName = nameof(CanCreateAndDeleteIndexAsync);
        var vectorSize = 1536;

        // Creates the index using IMemoryDb        
        await this.MemoryDb.CreateIndexAsync(indexName, vectorSize)
                           .ConfigureAwait(false);

        // Verifies the index is created using the ES client
        var actualIndexName = this.IndexNameHelper.Convert(nameof(CanCreateAndDeleteIndexAsync));
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
            this.IndexNameHelper.Convert(nameof(CanGetIndicesAsync) + "-First"),
            this.IndexNameHelper.Convert(nameof(CanGetIndicesAsync) + "-Second")
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
