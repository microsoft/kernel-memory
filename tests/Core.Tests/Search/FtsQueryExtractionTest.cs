// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Search.Query.Parsers;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests to debug FTS query extraction from parsed AST.
/// </summary>
public sealed class FtsQueryExtractionTest : IDisposable
{
    private readonly string _tempDir;

    public FtsQueryExtractionTest()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-fts-query-test-{Guid.NewGuid():N}");
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
    public async Task SimpleTextQuery_GeneratesCorrectFtsQuery()
    {
        // Arrange: Create real FTS index with known content
        var ftsDbPath = Path.Combine(this._tempDir, "fts.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: true, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "hello world").ConfigureAwait(false);

        // Test query: "hello" should generate FTS query that finds the content
        // Parse the query
        var queryNode = QueryParserFactory.Parse("hello");

        // Log what type of node was created
        var nodeType = queryNode.GetType().Name;

        // The NodeSearchService will extract FTS query from this node
        // We can't easily test the private ExtractFtsQuery method, but we can test end-to-end

        // Create a minimal storage
        var contentDbPath = Path.Combine(this._tempDir, "content.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        // Insert the content record
        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "hello world",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search using NodeSearchService (which will call ExtractFtsQuery internally)
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "hello",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.RecordId == "id1");
    }
}
