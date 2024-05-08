// Copyright (c) Microsoft. All rights reserved.

using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KM.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Elasticsearch.FunctionalTests.Additional;

public class IndexManagementTests : BaseFunctionalTestCase
{
    public IndexManagementTests(
        IConfiguration cfg,
        ITestOutputHelper output)
        : base(cfg, output)
    {
        this.Output = output;

#pragma warning disable KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var textEmbeddingGenerator = new OpenAITextEmbeddingGenerator(
            config: base.OpenAiConfig,
            textTokenizer: default,
            loggerFactory: default);
#pragma warning restore KMEXP01 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        this.Client = new ElasticsearchClient(base.ElasticsearchConfig.ToElasticsearchClientSettings());
        this.MemoryDb = new ElasticsearchMemory(base.ElasticsearchConfig, this.Client, textEmbeddingGenerator, default);
    }

    public ITestOutputHelper Output { get; }
    public ElasticsearchClient Client { get; }
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
        var idxHelper = new IndexNameHelper(base.ElasticsearchConfig);
        var actualIndexName = idxHelper.Convert(nameof(CanCreateAndDeleteIndexAsync));
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
        var idxNameHelper = new IndexNameHelper(base.ElasticsearchConfig);
        var indexNames = new[]
        {
            idxNameHelper.Convert(nameof(CanGetIndicesAsync) + "-First"),
            idxNameHelper.Convert(nameof(CanGetIndicesAsync) + "-Second")
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
