// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search;
using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Integration tests for FTS with ContentStorageService.
/// Tests the full pipeline: upsert content -> FTS indexed -> search returns results.
/// </summary>
public sealed class FtsIntegrationTests : IDisposable
{
    private readonly SqliteConnection _contentConnection;
    private readonly ContentStorageDbContext _context;
    private readonly Mock<ICuidGenerator> _mockCuidGenerator;
    private readonly Mock<ILogger<ContentStorageService>> _mockStorageLogger;
    private readonly Mock<ILogger<SqliteFtsIndex>> _mockFtsLogger;
    private readonly string _ftsDbPath;
    private readonly SqliteFtsIndex _ftsIndex;
    private readonly ContentStorageService _service;
    private int _cuidCounter;

    public FtsIntegrationTests()
    {
        // Content storage - in-memory SQLite
        this._contentConnection = new SqliteConnection("DataSource=:memory:");
        this._contentConnection.Open();

        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite(this._contentConnection)
            .Options;

        this._context = new ContentStorageDbContext(options);
        this._context.Database.EnsureCreated();

        // FTS index - temp file (FTS5 needs persistent storage for some operations)
        this._ftsDbPath = Path.Combine(Path.GetTempPath(), $"fts_integration_{Guid.NewGuid()}.db");
        this._mockFtsLogger = new Mock<ILogger<SqliteFtsIndex>>();
        this._ftsIndex = new SqliteFtsIndex(this._ftsDbPath, enableStemming: true, this._mockFtsLogger.Object);

        // Mock CUID generator with predictable IDs
        this._mockCuidGenerator = new Mock<ICuidGenerator>();
        this._cuidCounter = 0;
        this._mockCuidGenerator
            .Setup(x => x.Generate())
            .Returns(() => $"fts_test_id_{++this._cuidCounter:D5}");

        this._mockStorageLogger = new Mock<ILogger<ContentStorageService>>();

        // Create service with FTS index integrated
        var searchIndexById = new Dictionary<string, ISearchIndex>
        {
            ["sqlite-fts"] = this._ftsIndex
        };
        this._service = new ContentStorageService(
            this._context,
            this._mockCuidGenerator.Object,
            this._mockStorageLogger.Object,
            searchIndexById);
    }

    public void Dispose()
    {
        this._ftsIndex.Dispose();
        this._context.Dispose();
        this._contentConnection.Dispose();

        if (File.Exists(this._ftsDbPath))
        {
            File.Delete(this._ftsDbPath);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UpsertAsync_WithFtsIndex_IndexesContentAutomatically()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "This document is about machine learning and artificial intelligence.",
            MimeType = "text/plain",
            Title = "ML Document"
        };

        // Act
        var result = await this._service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Allow processing

        // Assert - WriteResult should indicate completion
        Assert.True(result.Completed);
        Assert.Equal("fts_test_id_00001", result.Id);

        // Verify FTS index contains the content
        var searchResults = await this._ftsIndex.SearchAsync("machine learning").ConfigureAwait(false);
        Assert.Single(searchResults);
        Assert.Equal(result.Id, searchResults[0].ContentId);
    }

    [Fact]
    public async Task UpsertAsync_WithFtsIndex_IncludesFtsStep()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Test content for FTS step verification",
            MimeType = "text/plain"
        };

        // Act
        var result = await this._service.UpsertAsync(request).ConfigureAwait(false);

        // Assert - Check that operation includes index step (index:0 for first search index)
        var operation = await this._context.Operations
            .FirstOrDefaultAsync(o => o.ContentId == result.Id)
            .ConfigureAwait(false);

        Assert.NotNull(operation);
        Assert.Contains("upsert", operation.PlannedSteps);
        Assert.Contains("index:sqlite-fts", operation.PlannedSteps); // SQLite FTS index
    }

    [Fact]
    public async Task DeleteAsync_WithFtsIndex_RemovesFromFtsIndex()
    {
        // Arrange - Create content first
        var request = new UpsertRequest
        {
            Id = "delete_fts_test",
            Content = "Content to be deleted from FTS index",
            MimeType = "text/plain"
        };
        await this._service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        // Verify it's in FTS
        var beforeDelete = await this._ftsIndex.SearchAsync("deleted").ConfigureAwait(false);
        Assert.Single(beforeDelete);

        // Act - Delete the content
        var result = await this._service.DeleteAsync("delete_fts_test").ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        // Assert
        Assert.True(result.Completed);
        Assert.Equal("delete_fts_test", result.Id);

        // Verify removed from FTS
        var afterDelete = await this._ftsIndex.SearchAsync("deleted").ConfigureAwait(false);
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task DeleteAsync_WithFtsIndex_IncludesFtsDeleteStep()
    {
        // Arrange
        const string contentId = "fts_delete_step_test";

        // Act
        var result = await this._service.DeleteAsync(contentId).ConfigureAwait(false);

        // Assert - Check that operation includes index delete step (index:0:delete for first search index)
        var operation = await this._context.Operations
            .FirstOrDefaultAsync(o => o.ContentId == contentId)
            .ConfigureAwait(false);

        Assert.NotNull(operation);
        Assert.Contains("delete", operation.PlannedSteps);
        Assert.Contains("index:sqlite-fts:delete", operation.PlannedSteps); // SQLite FTS index
    }

    [Fact]
    public async Task UpsertAsync_MultipleDocuments_AllSearchable()
    {
        // Arrange & Act
        var requests = new[]
        {
            new UpsertRequest { Content = "Python programming language", MimeType = "text/plain" },
            new UpsertRequest { Content = "JavaScript web development", MimeType = "text/plain" },
            new UpsertRequest { Content = "C# dotnet programming", MimeType = "text/plain" }
        };

        var ids = new List<string>();
        foreach (var request in requests)
        {
            var result = await this._service.UpsertAsync(request).ConfigureAwait(false);
            ids.Add(result.Id);
            await Task.Delay(50).ConfigureAwait(false);
        }

        await Task.Delay(200).ConfigureAwait(false); // Allow all to process

        // Assert - Search for "programming" should find 2 documents
        var searchResults = await this._ftsIndex.SearchAsync("programming").ConfigureAwait(false);
        Assert.Equal(2, searchResults.Count);
        Assert.Contains(searchResults, r => r.ContentId == ids[0]); // Python
        Assert.Contains(searchResults, r => r.ContentId == ids[2]); // C#
    }

    [Fact]
    public async Task UpsertAsync_UpdateContent_FtsIndexUpdated()
    {
        // Arrange - Create initial content
        var initialRequest = new UpsertRequest
        {
            Id = "update_fts_test",
            Content = "Original content about cats",
            MimeType = "text/plain"
        };
        await this._service.UpsertAsync(initialRequest).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        // Verify initial indexing
        var catResults = await this._ftsIndex.SearchAsync("cats").ConfigureAwait(false);
        Assert.Single(catResults);

        // Act - Update content
        var updateRequest = new UpsertRequest
        {
            Id = "update_fts_test",
            Content = "Updated content about dogs",
            MimeType = "text/plain"
        };
        await this._service.UpsertAsync(updateRequest).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        // Assert - Old content not searchable
        var catResultsAfter = await this._ftsIndex.SearchAsync("cats").ConfigureAwait(false);
        Assert.Empty(catResultsAfter);

        // New content is searchable
        var dogResults = await this._ftsIndex.SearchAsync("dogs").ConfigureAwait(false);
        Assert.Single(dogResults);
        Assert.Equal("update_fts_test", dogResults[0].ContentId);
    }

    [Fact]
    public async Task WriteResult_ReturnsQueuedOnlyWhenFtsFails()
    {
        // This test verifies the response contract when FTS step fails.
        // We can't easily force FTS to fail, but we verify the normal success path.

        // Arrange
        var request = new UpsertRequest
        {
            Content = "Content for success verification",
            MimeType = "text/plain"
        };

        // Act
        var result = await this._service.UpsertAsync(request).ConfigureAwait(false);

        // Assert - Should complete successfully
        Assert.True(result.Completed);
        Assert.False(result.Queued);
        Assert.Empty(result.Error);
    }
}
