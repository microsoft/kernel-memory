// Copyright (c) Free Mind Labs, Inc. All rights reserved.
using Elastic.Clients.Elasticsearch;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch;
using Microsoft.KernelMemory.MemoryStorage;
using UnitTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Elasticsearch.FunctionalTests._notported;

public class SearchTests : ElasticsearchTestBase
{
    public SearchTests(ITestOutputHelper output, IMemoryDb memoryDb, ITextEmbeddingGenerator textEmbeddingGenerator, ElasticsearchClient client, IIndexNameHelper indexNameHelper)
        : base(output, client, indexNameHelper)
    {
        this.MemoryDb = memoryDb ?? throw new ArgumentNullException(nameof(memoryDb));
        this.TextEmbeddingGenerator = textEmbeddingGenerator ?? throw new ArgumentNullException(nameof(textEmbeddingGenerator));
    }

    public IMemoryDb MemoryDb { get; }
    public ITextEmbeddingGenerator TextEmbeddingGenerator { get; }

    [Fact]
    public async Task CanGetListWithTagsAsync()
    {
        const int ExpectedTotalParagraphs = 4;

        // We upsert the file
        var docIds = await DataStorageTests.UpsertTextFilesAsync(
            memoryDb: this.MemoryDb,
            textEmbeddingGenerator: this.TextEmbeddingGenerator,
            output: this.Output,
            indexName: nameof(CanGetListWithTagsAsync),
                fileNames: new[]
                {
                    "Data/file1-Wikipedia-Carbon.txt",
                    "Data/file2-Wikipedia-Moon.txt"
                })
            .ConfigureAwait(false);

        // docsIds is a list of values like "d=3ed7b0787d484496ab25d50b2a887f8cf63193954fc844689116766434c11887//p=b84ee5e4841c4ab2877e30293752f7cc"
        Assert.Equal(expected: ExpectedTotalParagraphs, actual: docIds.Count());
        docIds = docIds.Select(x => x.Split("//")[0].Split("=")[1]).Distinct().ToList();

        this.Output.WriteLine($"Indexed returned the following ids:\n{string.Join("\n", docIds)}");

        var expectedDocs = docIds.Count();

        // Gets documents that are similar to the word "carbon" .
        var filter = new MemoryFilter();
        filter.Add("__file_type", "text/plain");
        filter.Add("__document_id", docIds.Select(x => (string?)x).ToList());

        var idx = 0;
        this.Output.WriteLine($"Filter: {filter.ToDebugString()}.\n");

        await foreach (var result in this.MemoryDb.GetListAsync(
            index: nameof(CanGetListWithTagsAsync),
            filters: new[] { filter },
            limit: 100,
            withEmbeddings: false))
        {
            var fileName = result.Payload["file"];
            this.Output.WriteLine($"Match #{idx++}: {fileName}");
        };

        Assert.Equal(expected: ExpectedTotalParagraphs, actual: idx);
    }

    [Fact]
    public async Task CanGetListWithEmptyFiltersAsync()
    {
        await foreach (var result in this.MemoryDb.GetListAsync(
            index: nameof(CanGetListWithTagsAsync),
            filters: new[] { new MemoryFilter() }, // <-- KM has a test to make sure this works.
            limit: 100,
            withEmbeddings: false))
        { };

        // If it gets here, the test passed.
    }
}

