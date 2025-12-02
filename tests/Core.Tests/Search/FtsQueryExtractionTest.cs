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

    // Issue #4: Tests for reserved words search functionality

    [Fact]
    public async Task QuotedNOTReservedWord_FindsDocumentContainingNOT()
    {
        // Issue #4: Should be able to search for the literal word "NOT"
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts-not.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: false, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "this is NOT important").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id2", "", "", "this is always important").ConfigureAwait(false);

        var contentDbPath = Path.Combine(this._tempDir, "content-not.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "this is NOT important",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id2",
            Content = "this is always important",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search for the literal word "NOT" (quoted in the query)
        var queryNode = QueryParserFactory.Parse("\"NOT\"");
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "\"NOT\"",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should find only the document containing "NOT"
        Assert.Single(results);
        Assert.Equal("id1", results[0].RecordId);
    }

    [Fact]
    public async Task QuotedANDReservedWord_FindsDocumentContainingAND()
    {
        // Issue #4: Should be able to search for the literal word "AND"
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts-and.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: false, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "Alice AND Bob").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id2", "", "", "Alice with Bob").ConfigureAwait(false);

        var contentDbPath = Path.Combine(this._tempDir, "content-and.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "Alice AND Bob",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id2",
            Content = "Alice with Bob",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search for the literal word "AND" (quoted in the query)
        var queryNode = QueryParserFactory.Parse("\"AND\"");
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "\"AND\"",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should find only the document containing "AND"
        Assert.Single(results);
        Assert.Equal("id1", results[0].RecordId);
    }

    [Fact]
    public async Task QuotedORReservedWord_FindsDocumentContainingOR()
    {
        // Issue #4: Should be able to search for the literal word "OR"
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts-or.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: false, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "yes OR no").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id2", "", "", "yes and no").ConfigureAwait(false);

        var contentDbPath = Path.Combine(this._tempDir, "content-or.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "yes OR no",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id2",
            Content = "yes and no",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search for the literal word "OR" (quoted in the query)
        var queryNode = QueryParserFactory.Parse("\"OR\"");
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "\"OR\"",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should find only the document containing "OR"
        Assert.Single(results);
        Assert.Equal("id1", results[0].RecordId);
    }

    [Fact]
    public async Task QuotedPhraseWithReservedWords_FindsDocumentWithExactPhrase()
    {
        // Issue #4: Should be able to search for phrases containing reserved words
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts-phrase.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: false, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "this AND that").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id2", "", "", "this or that").ConfigureAwait(false);

        var contentDbPath = Path.Combine(this._tempDir, "content-phrase.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "this AND that",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id2",
            Content = "this or that",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search for the phrase "this AND that"
        var queryNode = QueryParserFactory.Parse("\"this AND that\"");
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "\"this AND that\"",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should find only the document with the exact phrase
        Assert.Single(results);
        Assert.Equal("id1", results[0].RecordId);
    }

    [Fact]
    public async Task ReservedWordWithActualBooleanOperator_WorksCorrectly()
    {
        // Issue #4: Mixing quoted reserved words with actual boolean operators
        // Arrange
        var ftsDbPath = Path.Combine(this._tempDir, "fts-mixed.db");
        var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();

        using var ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: false, mockLogger.Object);
        await ftsIndex.IndexAsync("id1", "", "", "NOT important kubernetes").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id2", "", "", "important kubernetes").ConfigureAwait(false);
        await ftsIndex.IndexAsync("id3", "", "", "NOT important docker").ConfigureAwait(false);

        var contentDbPath = Path.Combine(this._tempDir, "content-mixed.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        using var context = new ContentStorageDbContext(options);
        context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var storage = new ContentStorageService(context, new CuidGenerator(), mockStorageLogger.Object);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id1",
            Content = "NOT important kubernetes",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id2",
            Content = "important kubernetes",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        await storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Id = "id3",
            Content = "NOT important docker",
            MimeType = "text/plain"
        }, CancellationToken.None).ConfigureAwait(false);

        // Act: Search for "NOT" (literal) AND kubernetes (must contain both)
        var queryNode = QueryParserFactory.Parse("\"NOT\" AND kubernetes");
        var nodeService = new NodeSearchService("test", ftsIndex, storage);
        var searchRequest = new SearchRequest
        {
            Query = "\"NOT\" AND kubernetes",
            Limit = 10,
            MinRelevance = 0.0f
        };

        var (results, _) = await nodeService.SearchAsync(queryNode, searchRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert: Should find only the document containing literal "NOT" AND "kubernetes"
        Assert.Single(results);
        Assert.Equal("id1", results[0].RecordId);
    }
}
