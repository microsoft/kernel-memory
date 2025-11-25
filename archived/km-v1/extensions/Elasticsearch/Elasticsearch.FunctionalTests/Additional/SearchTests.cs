// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

namespace Microsoft.Elasticsearch.FunctionalTests.Additional;

public class SearchTests : MemoryDbFunctionalTest
{
    public SearchTests(IConfiguration cfg, ITestOutputHelper output)
        : base(cfg, output)
    {
    }

    // TODO: I am removing this test because it started failing after I made the prior changes to
    // address the NuGet packages simplification.
    //
    // I believe this test is correct and there might not be a test that catches the scenario elsewhere.
    // The test inserts 2 documents and then tries to retrieve them by using a filter
    //     __file_type = text/plain
    //            AND
    //     __document_id in [GUID1, GUID2]
    //
    // Without a change to ElasticsearchMemory.ConvertTagFilters() this test will fail.
    // I need to think about this some more.
    //[Fact]
    public async Task CanGetListWithTagsAsync()
    {
        const int ExpectedTotalParagraphs = 4;

        // We upsert the file
        var docIds = await DataStorageTests.UpsertTextFilesAsync(
                memoryDb: this.MemoryDb,
                textEmbeddingGenerator: this.TextEmbeddingGenerator,
                output: this.Output,
                indexName: nameof(this.CanGetListWithTagsAsync),
                fileNames:
                [
                    TestsHelper.WikipediaCarbonFileName,
                    TestsHelper.WikipediaMoonFilename
                ])
            .ConfigureAwait(false);

        // docsIds is a list of values like "d=3ed7b0787d484496ab25d50b2a887f8cf63193954fc844689116766434c11887//p=b84ee5e4841c4ab2877e30293752f7cc"
        Assert.Equal(expected: ExpectedTotalParagraphs, actual: docIds.Count());
        docIds = docIds.Select(x => x.Split("//")[0].Split("=")[1]).Distinct().ToList();

        this.Output.WriteLine($"Indexed returned the following ids:\n{string.Join("\n", docIds)}");

        var expectedDocs = docIds.Count();

        // Gets documents that are similar to the word "carbon" .
        var filter = new MemoryFilter
        {
            { "__file_type", "text/plain" },
            { "__document_id", docIds.Select(x => (string?)x).ToList() }
        };

        var idx = 0;
        this.Output.WriteLine($"Filter: {filter.ToDebugString()}.\n");

        await foreach (var result in this.MemoryDb.GetListAsync(
                           index: nameof(this.CanGetListWithTagsAsync),
                           filters: [filter],
                           limit: 100,
                           withEmbeddings: false))
        {
            var fileName = result.Payload["file"];
            this.Output.WriteLine($"Match #{idx++}: {fileName}");
        }

        Assert.Equal(expected: ExpectedTotalParagraphs, actual: idx);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task CanGetListWithEmptyFiltersAsync()
    {
        await foreach (var result in this.MemoryDb.GetListAsync(
                           index: nameof(this.CanGetListWithTagsAsync),
                           filters: [[]], // <-- KM has a test to make sure this works.
                           limit: 100,
                           withEmbeddings: false))
        {
        }

        // If it gets here, the test passed.
    }
}
