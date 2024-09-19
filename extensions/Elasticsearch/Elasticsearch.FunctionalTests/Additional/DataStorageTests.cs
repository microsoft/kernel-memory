// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;
using Microsoft.KernelMemory.MemoryStorage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Elasticsearch.FunctionalTests.Additional;

public class DataStorageTests : MemoryDbFunctionalTest
{
    public DataStorageTests(
        IConfiguration cfg,
        ITestOutputHelper output)
        : base(cfg, output)
    {
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task CanUpsertOneTextDocumentAndDeleteAsync()
    {
        // We upsert the file
        var docIds = await UpsertTextFilesAsync(
            memoryDb: this.MemoryDb,
            textEmbeddingGenerator: this.TextEmbeddingGenerator,
            output: this.Output,
            indexName: nameof(this.CanUpsertOneTextDocumentAndDeleteAsync),
            fileNames: new[]
            {
                TestsHelper.WikipediaCarbonFileName
            }).ConfigureAwait(false);

        // Deletes the document
        var deletes = docIds.Select(id => new MemoryRecord()
        {
            Id = id
        });

        foreach (var deleteRec in deletes)
        {
            await this.MemoryDb.DeleteAsync(nameof(this.CanUpsertOneTextDocumentAndDeleteAsync), deleteRec)
                .ConfigureAwait(false);
        }

        // Verifies that the documents are gone
        var indexName = IndexNameHelper.Convert(nameof(this.CanUpsertOneTextDocumentAndDeleteAsync), base.ElasticsearchConfig);
        var res = await this.Client.CountAsync(r => r.Index(indexName))
            .ConfigureAwait(false);
        Assert.Equal(0, res.Count);
    }

    [Fact]
    [Trait("Category", "Elasticsearch")]
    public async Task CanUpsertTwoTextFilesAndGetSimilarListAsync()
    {
        await UpsertTextFilesAsync(
            memoryDb: this.MemoryDb,
            textEmbeddingGenerator: this.TextEmbeddingGenerator,
            output: this.Output,
            indexName: nameof(this.CanUpsertTwoTextFilesAndGetSimilarListAsync),
            fileNames: new[]
            {
                TestsHelper.WikipediaCarbonFileName,
                TestsHelper.WikipediaMoonFilename
            }).ConfigureAwait(false);

        // Gets documents that are similar to the word "carbon" .
        var foundSomething = false;

        var textToMatch = "carbon";
        await foreach (var result in this.MemoryDb.GetSimilarListAsync(
                           index: nameof(this.CanUpsertTwoTextFilesAndGetSimilarListAsync),
                           text: textToMatch,
                           limit: 1))
        {
            this.Output.WriteLine($"Found a document matching '{textToMatch}': {result.Item1.Payload["file"]}.");
            return;
        }

        ;

        Assert.True(foundSomething, "It should have found something...");
    }

    public static string GuidWithoutDashes() => Guid.NewGuid().ToString().Replace("-", "", StringComparison.OrdinalIgnoreCase).ToLower(CultureInfo.CurrentCulture);

    public static async Task<IEnumerable<string>> UpsertTextFilesAsync(
        IMemoryDb memoryDb,
        ITextEmbeddingGenerator textEmbeddingGenerator,
        ITestOutputHelper output,
        string indexName,
        IEnumerable<string> fileNames)
    {
        ArgumentNullException.ThrowIfNull(memoryDb);
        ArgumentNullException.ThrowIfNull(textEmbeddingGenerator);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(indexName);
        ArgumentNullException.ThrowIfNull(fileNames);

        // IMemoryDb does not create the index automatically.
        await memoryDb.CreateIndexAsync(indexName, 1536)
            .ConfigureAwait(false);

        var results = new List<string>();
        foreach (var fileName in fileNames)
        {
            // Reads the text from the file
            string fullText = await File.ReadAllTextAsync(fileName)
                .ConfigureAwait(false);

            // Splits the text into lines of up to 1000 tokens each
#pragma warning disable KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var lines = TextChunker.SplitPlainTextLines(fullText,
                maxTokensPerLine: 1000,
                tokenCounter: null);

            // Splits the line into paragraphs
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines,
                maxTokensPerParagraph: 1000,
                overlapTokens: 100);
#pragma warning restore KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            output.WriteLine($"File '{fileName}' contains {paragraphs.Count} paragraphs.");

            // Indexes each paragraph as a separate document
            var paraIdx = 0;
            var documentId = GuidWithoutDashes() + GuidWithoutDashes();
            var fileId = GuidWithoutDashes();

            foreach (var paragraph in paragraphs)
            {
                var embedding = await textEmbeddingGenerator.GenerateEmbeddingAsync(paragraph)
                    .ConfigureAwait(false);

                output.WriteLine($"Indexed paragraph {++paraIdx}/{paragraphs.Count}. {paragraph.Length} characters.");

                var filePartId = GuidWithoutDashes();

                var esId = $"d={documentId}//p={filePartId}";

                var memoryRecord = new MemoryRecord()
                {
                    Id = esId,
                    Payload = new Dictionary<string, object>()
                    {
                        { "file", fileName },
                        { "text", paragraph },
                        { "vector_provider", textEmbeddingGenerator.GetType().Name },
                        { "vector_generator", "TODO" },
                        { "last_update", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") },
                        { "text_embedding_generator", textEmbeddingGenerator.GetType().Name }
                    },
                    Tags = new TagCollection()
                    {
                        { "__document_id", documentId },
                        { "__file_type", "text/plain" },
                        { "__file_id", fileId },
                        { "__file_part", filePartId }
                    },
                    Vector = embedding
                };

                var res = await memoryDb.UpsertAsync(indexName, memoryRecord)
                    .ConfigureAwait(false);

                results.Add(res);
            }

            output.WriteLine("");
        }

        return results;
    }
}
