// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Simple end-to-end test of the search pipeline to debug the "no results" issue.
/// </summary>
public sealed class SimpleSearchTest : IDisposable
{
    private readonly string _tempDir;

    public SimpleSearchTest()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-simple-search-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, true);
            }
        }
        catch (IOException)
        {
            // Ignore
        }
    }

    [Fact]
    public async Task SimpleTextSearch_AfterDirectFtsIndexing_ShouldFindResults()
    {
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts.db");
        var contentDbPath = Path.Combine(this._tempDir, "content.db");

        var mockFtsLogger = new Mock<ILogger<SqliteFtsIndex>>();
        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();

        // Index directly to FTS
        using (var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: true, mockFtsLogger.Object))
        {
            await ftsIndex.IndexAsync("id1", title: "", description: "", content: "ciao mondo").ConfigureAwait(false);
        }

        // Create content storage with the content
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var cuidGen = new CuidGenerator();
        var storage = new ContentStorageService(context, cuidGen, mockStorageLogger.Object);

        // Insert the content record so it can be retrieved
        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "ciao mondo",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Create search services
        using var ftsIndex2 = new SqliteFtsIndex(ftsDbPath, enableStemming: true, mockFtsLogger.Object);
        var nodeService = new NodeSearchService("test", ftsIndex2, storage);
        var searchService = new SearchService(new Dictionary<string, NodeSearchService> { ["test"] = nodeService });

        // Act: Search for "ciao"
        var result = await searchService.SearchAsync(new SearchRequest
        {
            Query = "ciao",
            Limit = 10,
            MinRelevance = 0.0f
        }, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalResults > 0, $"Expected results but got {result.TotalResults}");
        Assert.NotEmpty(result.Results);
    }
}
