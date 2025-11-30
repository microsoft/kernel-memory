// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Unit tests for SqliteFtsIndex using in-memory SQLite database.
/// Tests cover indexing, searching, and removal operations.
/// </summary>
public sealed class SqliteFtsIndexTests : IDisposable
{
    private readonly string _dbPath;
    private readonly Mock<ILogger<SqliteFtsIndex>> _mockLogger;
    private readonly SqliteFtsIndex _ftsIndex;

    public SqliteFtsIndexTests()
    {
        // Use temp file for SQLite (FTS5 doesn't work well with :memory: across connections)
        this._dbPath = Path.Combine(Path.GetTempPath(), $"fts_test_{Guid.NewGuid()}.db");
        this._mockLogger = new Mock<ILogger<SqliteFtsIndex>>();
        this._ftsIndex = new SqliteFtsIndex(this._dbPath, enableStemming: true, this._mockLogger.Object);
    }

    public void Dispose()
    {
        this._ftsIndex.Dispose();

        // Clean up temp file
        if (File.Exists(this._dbPath))
        {
            File.Delete(this._dbPath);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IndexAsync_IndexesContentSuccessfully()
    {
        // Arrange
        const string contentId = "test-id-1";
        const string text = "This is a test document for full text search.";

        // Act
        await this._ftsIndex.IndexAsync(contentId, text).ConfigureAwait(false);

        // Assert - Search should find it
        var results = await this._ftsIndex.SearchAsync("test document").ConfigureAwait(false);
        Assert.Single(results);
        Assert.Equal(contentId, results[0].ContentId);
    }

    [Fact]
    public async Task IndexAsync_ReplacesExistingContent()
    {
        // Arrange
        const string contentId = "test-id-replace";
        await this._ftsIndex.IndexAsync(contentId, "original content about cats").ConfigureAwait(false);

        // Act - Replace with new content
        await this._ftsIndex.IndexAsync(contentId, "updated content about dogs").ConfigureAwait(false);

        // Assert - Old content should not be found
        var catResults = await this._ftsIndex.SearchAsync("cats").ConfigureAwait(false);
        Assert.Empty(catResults);

        // New content should be found
        var dogResults = await this._ftsIndex.SearchAsync("dogs").ConfigureAwait(false);
        Assert.Single(dogResults);
        Assert.Equal(contentId, dogResults[0].ContentId);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForNoMatches()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "content about programming").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("nonexistent xyz123").ConfigureAwait(false);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForEmptyQuery()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "some content").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("").ConfigureAwait(false);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMultipleMatches()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "The quick brown fox jumps").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id2", "A quick rabbit runs fast").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id3", "Slow turtle walks slowly").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("quick").ConfigureAwait(false);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ContentId == "id1");
        Assert.Contains(results, r => r.ContentId == "id2");
    }

    [Fact]
    public async Task SearchAsync_RespectsLimit()
    {
        // Arrange - Create many documents
        for (int i = 0; i < 20; i++)
        {
            await this._ftsIndex.IndexAsync($"id{i}", $"Document number {i} with common word test").ConfigureAwait(false);
        }

        // Act
        var results = await this._ftsIndex.SearchAsync("test", limit: 5).ConfigureAwait(false);

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task SearchAsync_OrdersByRelevance()
    {
        // Arrange - Create documents with different relevance
        await this._ftsIndex.IndexAsync("low", "This document mentions search once.").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("high", "Search search search - this document is all about search optimization for search engines.").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("medium", "Search results and search queries are important for search.").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("search").ConfigureAwait(false);

        // Assert - Higher relevance (more occurrences) should come first
        Assert.Equal(3, results.Count);
        Assert.Equal("high", results[0].ContentId); // Most occurrences
    }

    [Fact]
    public async Task SearchAsync_ReturnsSnippets()
    {
        // Arrange
        const string longText = "This is a very long document. It contains many words and sentences. " +
                                "The important keyword appears here. More text follows after the keyword. " +
                                "And even more text to make this document quite lengthy.";
        await this._ftsIndex.IndexAsync("id1", longText).ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("keyword").ConfigureAwait(false);

        // Assert
        Assert.Single(results);
        Assert.NotEmpty(results[0].Snippet);
        Assert.Contains("keyword", results[0].Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_WithStemming_FindsRelatedWords()
    {
        // Arrange - Stemming should match "running" with "run"
        // Note: Porter stemmer handles regular inflections (running->run, runs->run)
        // but NOT irregular verbs (ran does NOT stem to run)
        await this._ftsIndex.IndexAsync("id1", "The athlete is running fast").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id2", "She runs every morning").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id3", "The runner finished the race").ConfigureAwait(false);

        // Act - Search for "run" should find variations via stemming
        var results = await this._ftsIndex.SearchAsync("run").ConfigureAwait(false);

        // Assert - With Porter stemming: running->run, runs->run, runner->runner (partial match)
        Assert.Equal(2, results.Count); // running and runs stem to run
        Assert.Contains(results, r => r.ContentId == "id1");
        Assert.Contains(results, r => r.ContentId == "id2");
    }

    [Fact]
    public async Task RemoveAsync_RemovesIndexedContent()
    {
        // Arrange
        const string contentId = "test-remove";
        await this._ftsIndex.IndexAsync(contentId, "content to be removed").ConfigureAwait(false);

        // Verify it exists
        var beforeRemove = await this._ftsIndex.SearchAsync("removed").ConfigureAwait(false);
        Assert.Single(beforeRemove);

        // Act
        await this._ftsIndex.RemoveAsync(contentId).ConfigureAwait(false);

        // Assert
        var afterRemove = await this._ftsIndex.SearchAsync("removed").ConfigureAwait(false);
        Assert.Empty(afterRemove);
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent()
    {
        // Arrange
        const string contentId = "non-existent-id";

        // Act - Should not throw for non-existent content
        await this._ftsIndex.RemoveAsync(contentId).ConfigureAwait(false);
        await this._ftsIndex.RemoveAsync(contentId).ConfigureAwait(false);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllContent()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "first document").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id2", "second document").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id3", "third document").ConfigureAwait(false);

        // Verify content exists
        var beforeClear = await this._ftsIndex.SearchAsync("document").ConfigureAwait(false);
        Assert.Equal(3, beforeClear.Count);

        // Act
        await this._ftsIndex.ClearAsync().ConfigureAwait(false);

        // Assert
        var afterClear = await this._ftsIndex.SearchAsync("document").ConfigureAwait(false);
        Assert.Empty(afterClear);
    }

    [Fact]
    public async Task SearchAsync_HandlesFts5Syntax()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "apple banana cherry").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id2", "apple orange grape").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id3", "banana mango papaya").ConfigureAwait(false);

        // Act - FTS5 AND query
        var results = await this._ftsIndex.SearchAsync("apple AND banana").ConfigureAwait(false);

        // Assert - Only id1 has both apple and banana
        Assert.Single(results);
        Assert.Equal("id1", results[0].ContentId);
    }

    [Fact]
    public async Task SearchAsync_HandlesSpecialCharacters()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "C# programming with .NET framework").ConfigureAwait(false);
        await this._ftsIndex.IndexAsync("id2", "JavaScript ES6+ features").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("programming").ConfigureAwait(false);

        // Assert
        Assert.Single(results);
        Assert.Equal("id1", results[0].ContentId);
    }

    [Fact]
    public async Task IndexAsync_HandlesUnicodeContent()
    {
        // Arrange
        const string unicodeContent = "日本語テキスト and English mixed content";
        await this._ftsIndex.IndexAsync("unicode-id", unicodeContent).ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("English").ConfigureAwait(false);

        // Assert
        Assert.Single(results);
        Assert.Equal("unicode-id", results[0].ContentId);
    }

    [Fact]
    public async Task IndexAsync_HandlesEmptyContent()
    {
        // Arrange & Act - Should not throw
        await this._ftsIndex.IndexAsync("empty-id", "").ConfigureAwait(false);

        // Assert - Search should return nothing for empty content
        var results = await this._ftsIndex.SearchAsync("anything").ConfigureAwait(false);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScoreProperty_IsPositive()
    {
        // Arrange
        await this._ftsIndex.IndexAsync("id1", "test content for scoring").ConfigureAwait(false);

        // Act
        var results = await this._ftsIndex.SearchAsync("test").ConfigureAwait(false);

        // Assert - Score should be positive (we negate FTS5 rank)
        Assert.Single(results);
        Assert.True(results[0].Score > 0, "Score should be positive");
    }
}
