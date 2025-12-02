// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search;
using KernelMemory.Core.Search.Models;
using KernelMemory.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// End-to-end integration tests for search functionality.
/// Tests insert real data, execute complex queries (infix AND MongoDB JSON), and verify actual results.
/// These tests exercise the full pipeline: insert → index → parse → extract FTS → search → rerank → filter.
/// </summary>
public sealed class SearchEndToEndTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SearchService _searchService;
    private readonly ContentStorageService _storage;
    private readonly Dictionary<string, string> _insertedIds;
    private readonly ContentStorageDbContext _context;
    private readonly SqliteFtsIndex _ftsIndex;

    public SearchEndToEndTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-e2e-search-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);

        // Setup storage and FTS
        var contentDbPath = Path.Combine(this._tempDir, "content.db");
        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={contentDbPath}")
            .Options;
        this._context = new ContentStorageDbContext(options);
        this._context.Database.EnsureCreated();

        var mockStorageLogger = new Mock<ILogger<ContentStorageService>>();
        var mockFtsLogger = new Mock<ILogger<SqliteFtsIndex>>();
        var cuidGenerator = new CuidGenerator();

        var ftsDbPath = Path.Combine(this._tempDir, "fts.db");
        this._ftsIndex = new SqliteFtsIndex(ftsDbPath, enableStemming: true, mockFtsLogger.Object);
        var searchIndexes = new Dictionary<string, ISearchIndex> { ["fts"] = this._ftsIndex };

        this._storage = new ContentStorageService(this._context, cuidGenerator, mockStorageLogger.Object, searchIndexes);
        var nodeService = new NodeSearchService("test-node", this._ftsIndex, this._storage);
        this._searchService = new SearchService(new Dictionary<string, NodeSearchService> { ["test-node"] = nodeService });

        this._insertedIds = new Dictionary<string, string>();
    }

    public void Dispose()
    {
        this._ftsIndex.Dispose();
        this._context.Dispose();

        try
        {
            if (Directory.Exists(this._tempDir))
            {
                Directory.Delete(this._tempDir, true);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Helper to insert content and track its ID by a key.
    /// </summary>
    /// <param name="key">Key to track the inserted ID.</param>
    /// <param name="content">Content to insert.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="tags">Optional tags.</param>
    private async Task InsertAsync(string key, string content, string? title = null, string? description = null, string[]? tags = null)
    {
        var result = await this._storage.UpsertAsync(new KernelMemory.Core.Storage.Models.UpsertRequest
        {
            Content = content,
            Title = title ?? string.Empty,
            Description = description ?? string.Empty,
            MimeType = "text/plain",
            Tags = tags ?? []
        }, CancellationToken.None).ConfigureAwait(false);

        this._insertedIds[key] = result.Id;
        Assert.True(result.Completed, $"Insert '{key}' failed to complete");
    }

    /// <summary>
    /// Helper to execute search and return results.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="minRelevance">Minimum relevance threshold.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <returns>Search response.</returns>
    private async Task<SearchResponse> SearchAsync(string query, float minRelevance = 0.0f, int limit = 20)
    {
        return await this._searchService.SearchAsync(new SearchRequest
        {
            Query = query,
            Limit = limit,
            MinRelevance = minRelevance
        }, CancellationToken.None).ConfigureAwait(false);
    }

    #region Infix Notation Tests

    [Fact]
    public async Task InfixQuery_SimpleText_FindsMatchingContent()
    {
        // Arrange
        await this.InsertAsync("doc1", "kubernetes deployment guide").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker container basics").ConfigureAwait(false);
        await this.InsertAsync("doc3", "python programming tutorial").ConfigureAwait(false);

        // Act: Simple text search
        var response = await this.SearchAsync("kubernetes").ConfigureAwait(false);

        // Assert: Verify actual results
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("kubernetes", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InfixQuery_BooleanAnd_FindsOnlyMatchingBoth()
    {
        // Arrange
        await this.InsertAsync("doc1", "kubernetes and docker together").ConfigureAwait(false);
        await this.InsertAsync("doc2", "only kubernetes here").ConfigureAwait(false);
        await this.InsertAsync("doc3", "only docker here").ConfigureAwait(false);

        // Act: AND operator
        var response = await this.SearchAsync("kubernetes AND docker").ConfigureAwait(false);

        // Assert: Only doc1 should match
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("kubernetes", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InfixQuery_BooleanOr_FindsMatchingEither()
    {
        // Arrange
        await this.InsertAsync("doc1", "python programming").ConfigureAwait(false);
        await this.InsertAsync("doc2", "javascript development").ConfigureAwait(false);
        await this.InsertAsync("doc3", "java enterprise").ConfigureAwait(false);

        // Act: OR operator
        var response = await this.SearchAsync("python OR javascript").ConfigureAwait(false);

        // Assert: doc1 and doc2 should match
        Assert.Equal(2, response.TotalResults);
        Assert.Equal(2, response.Results.Length);

        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.Contains(this._insertedIds["doc1"], resultIds);
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.DoesNotContain(this._insertedIds["doc3"], resultIds);
    }

    // NOTE: NOT operator test removed - FTS NOT handling is complex and needs separate investigation
    // SQLite FTS5 NOT support is limited, and the current implementation may not filter correctly
    // in all cases. This is a known limitation that needs dedicated testing and possibly
    // moving NOT filtering entirely to LINQ post-processing instead of FTS.

    [Fact]
    public async Task InfixQuery_NestedParentheses_EvaluatesCorrectly()
    {
        // Arrange
        await this.InsertAsync("doc1", "docker and kubernetes tutorial").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker and helm charts").ConfigureAwait(false);
        await this.InsertAsync("doc3", "terraform and kubernetes").ConfigureAwait(false);
        await this.InsertAsync("doc4", "ansible automation").ConfigureAwait(false);

        // Act: Complex nested query
        var response = await this.SearchAsync("docker AND (kubernetes OR helm)").ConfigureAwait(false);

        // Assert: doc1 and doc2 should match
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.Contains(this._insertedIds["doc1"], resultIds);
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.DoesNotContain(this._insertedIds["doc3"], resultIds); // Has kubernetes but no docker
        Assert.DoesNotContain(this._insertedIds["doc4"], resultIds); // Has neither
    }

    [Fact]
    public async Task InfixQuery_FieldSpecificContent_FindsOnlyContentMatches()
    {
        // Arrange
        await this.InsertAsync("doc1", "database configuration", "Setup Guide").ConfigureAwait(false);
        await this.InsertAsync("doc2", "user authentication", "Database Configuration").ConfigureAwait(false);

        // Act: Search specifically in content field
        var response = await this.SearchAsync("content:database").ConfigureAwait(false);

        // Assert: Only doc1 (has "database" in content, not title)
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("database", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InfixQuery_FieldSpecificTitle_FindsOnlyTitleMatches()
    {
        // Arrange
        await this.InsertAsync("doc1", "how to configure docker", "Docker Tutorial").ConfigureAwait(false);
        await this.InsertAsync("doc2", "kubernetes deployment with docker", "Kubernetes Guide").ConfigureAwait(false);

        // Act: Search specifically in title field
        var response = await this.SearchAsync("title:docker").ConfigureAwait(false);

        // Assert: Only doc1 (has "docker" in title)
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("docker", response.Results[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InfixQuery_MultipleFieldsWithBoolean_FindsCorrectMatches()
    {
        // Arrange
        await this.InsertAsync("doc1", "api documentation", "REST API Guide", "Complete guide to REST APIs").ConfigureAwait(false);
        await this.InsertAsync("doc2", "graphql tutorial", "GraphQL API", "Learn GraphQL APIs").ConfigureAwait(false);
        await this.InsertAsync("doc3", "database setup", "Database Guide", "Setup your database").ConfigureAwait(false);

        // Act: Complex query across multiple fields
        var response = await this.SearchAsync("(title:api OR description:api) AND content:documentation").ConfigureAwait(false);

        // Assert: Only doc1 matches (has "api" in title AND "documentation" in content)
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task InfixQuery_StemmingOnContent_FindsWordVariations()
    {
        // Arrange
        await this.InsertAsync("doc1", "summary of the meeting").ConfigureAwait(false);
        await this.InsertAsync("doc2", "detailed report").ConfigureAwait(false);

        // Act: Search for "summaries" (plural) should find "summary" (singular)
        var response = await this.SearchAsync("content:summaries").ConfigureAwait(false);

        // Assert: Stemming should match
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("summary", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InfixQuery_StemmingOnTitle_FindsWordVariations()
    {
        // Arrange
        await this.InsertAsync("doc1", "guide content", "Connect to server").ConfigureAwait(false);
        await this.InsertAsync("doc2", "other content", "Setup instructions").ConfigureAwait(false);

        // Act: Search for "connection" should find "connect"
        var response = await this.SearchAsync("title:connection").ConfigureAwait(false);

        // Assert
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task InfixQuery_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        await this.InsertAsync("doc1", "Kubernetes Tutorial").ConfigureAwait(false);
        await this.InsertAsync("doc2", "DOCKER GUIDE").ConfigureAwait(false);

        // Act: Search with different casing
        var response1 = await this.SearchAsync("KUBERNETES").ConfigureAwait(false);
        var response2 = await this.SearchAsync("kubernetes").ConfigureAwait(false);
        var response3 = await this.SearchAsync("KuBeRnEtEs").ConfigureAwait(false);

        // Assert: All should find doc1
        Assert.Equal(1, response1.TotalResults);
        Assert.Equal(1, response2.TotalResults);
        Assert.Equal(1, response3.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response1.Results[0].Id);
        Assert.Equal(this._insertedIds["doc1"], response2.Results[0].Id);
        Assert.Equal(this._insertedIds["doc1"], response3.Results[0].Id);
    }

    [Fact]
    public async Task InfixQuery_DefaultMinRelevance_FiltersLowScores()
    {
        // Arrange
        await this.InsertAsync("doc1", "machine learning algorithms").ConfigureAwait(false);
        await this.InsertAsync("doc2", "deep learning networks").ConfigureAwait(false);

        // Act: Search with default MinRelevance (0.3)
        var response = await this.SearchAsync("learning", minRelevance: 0.3f).ConfigureAwait(false);

        // Assert: Should find results (regression test for BM25 normalization bug)
        Assert.True(response.TotalResults > 0, "BM25 normalization bug: scores should be >= 0.3 after normalization");
        Assert.NotEmpty(response.Results);
        Assert.All(response.Results, r => Assert.True(r.Relevance >= 0.3f));
    }

    [Fact]
    public async Task InfixQuery_ThreeFieldQuery_FindsAcrossAllFields()
    {
        // Arrange
        await this.InsertAsync("doc1", "content about apis", "API Development", "REST API guide").ConfigureAwait(false);
        await this.InsertAsync("doc2", "python code", "Python Tutorial", "Learn python basics").ConfigureAwait(false);
        await this.InsertAsync("doc3", "docker commands", "Container Guide", "Docker tutorial").ConfigureAwait(false);

        // Act: Search across all three FTS fields
        var response = await this.SearchAsync("title:api OR description:api OR content:api").ConfigureAwait(false);

        // Assert: Only doc1 has "api" in any field
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    #endregion

    #region MongoDB JSON Query Tests

    [Fact]
    public async Task MongoQuery_SimpleFieldEquals_FindsMatch()
    {
        // Arrange
        await this.InsertAsync("doc1", "kubernetes orchestration").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker containers").ConfigureAwait(false);

        // Act: MongoDB JSON syntax
        var response = await this.SearchAsync("{\"content\": \"kubernetes\"}").ConfigureAwait(false);

        // Assert
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
        Assert.Contains("kubernetes", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MongoQuery_AndOperator_FindsOnlyMatchingBoth()
    {
        // Arrange
        await this.InsertAsync("doc1", "docker and kubernetes").ConfigureAwait(false);
        await this.InsertAsync("doc2", "only docker").ConfigureAwait(false);
        await this.InsertAsync("doc3", "only kubernetes").ConfigureAwait(false);

        // Act: MongoDB $and
        var response = await this.SearchAsync("{\"$and\": [{\"content\": \"docker\"}, {\"content\": \"kubernetes\"}]}").ConfigureAwait(false);

        // Assert
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task MongoQuery_OrOperator_FindsMatchingEither()
    {
        // Arrange
        await this.InsertAsync("doc1", "python code").ConfigureAwait(false);
        await this.InsertAsync("doc2", "javascript code").ConfigureAwait(false);
        await this.InsertAsync("doc3", "java code").ConfigureAwait(false);

        // Act: MongoDB $or
        var response = await this.SearchAsync("{\"$or\": [{\"content\": \"python\"}, {\"content\": \"javascript\"}]}").ConfigureAwait(false);

        // Assert: doc1 and doc2
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.Contains(this._insertedIds["doc1"], resultIds);
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.DoesNotContain(this._insertedIds["doc3"], resultIds);
    }

    [Fact]
    public async Task MongoQuery_TextSearchOperator_FindsTextMatches()
    {
        // Arrange
        await this.InsertAsync("doc1", "full text search capabilities").ConfigureAwait(false);
        await this.InsertAsync("doc2", "vector search features").ConfigureAwait(false);

        // Act: MongoDB $text operator
        var response = await this.SearchAsync("{\"$text\": {\"$search\": \"full text\"}}").ConfigureAwait(false);

        // Assert
        Assert.True(response.TotalResults > 0);
        Assert.Contains(response.Results, r => r.Id == this._insertedIds["doc1"]);
    }

    [Fact]
    public async Task MongoQuery_FieldSpecificWithStemming_FindsVariations()
    {
        // Arrange
        await this.InsertAsync("doc1", "development guide", "Develop Features").ConfigureAwait(false);
        await this.InsertAsync("doc2", "deployment process", "Deploy Apps").ConfigureAwait(false);

        // Act: Search for "development" in title should find "develop"
        var response = await this.SearchAsync("{\"title\": \"development\"}").ConfigureAwait(false);

        // Assert
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task MongoQuery_ComplexNestedLogic_FindsCorrectMatches()
    {
        // Arrange
        await this.InsertAsync("doc1", "api documentation for rest services", "REST API").ConfigureAwait(false);
        await this.InsertAsync("doc2", "graphql api tutorial", "GraphQL Guide").ConfigureAwait(false);
        await this.InsertAsync("doc3", "database rest interface", "Database API").ConfigureAwait(false);

        // Act: Complex MongoDB query: (title has "api") AND (content has "rest" OR content has "graphql")
        var response = await this.SearchAsync(
            "{\"$and\": [{\"title\": \"api\"}, {\"$or\": [{\"content\": \"rest\"}, {\"content\": \"graphql\"}]}]}"
        ).ConfigureAwait(false);

        // Assert: doc1 has title "REST API" with "api" + content has "rest"  ✓
        //         doc2 has title "GraphQL Guide" without "api" + content has "graphql" ✗
        //         doc3 has title "Database API" with "api" + content has "rest" ✓
        // So doc1 and doc3 should match
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.Contains(this._insertedIds["doc1"], resultIds);
        Assert.Contains(this._insertedIds["doc3"], resultIds);
        Assert.DoesNotContain(this._insertedIds["doc2"], resultIds); // Title lacks "api"
    }

    [Fact]
    public async Task MongoQuery_MultipleFieldsInAnd_AllMustMatch()
    {
        // Arrange
        await this.InsertAsync("doc1", "docker tutorial content", "Docker Guide", "Learn docker containers").ConfigureAwait(false);
        await this.InsertAsync("doc2", "kubernetes guide content", "Docker Tutorial", "No description").ConfigureAwait(false);
        await this.InsertAsync("doc3", "general content", "General Guide", "Docker and kubernetes").ConfigureAwait(false);

        // Act: Must have docker in title AND content AND description
        var response = await this.SearchAsync(
            "{\"$and\": [{\"title\": \"docker\"}, {\"content\": \"docker\"}, {\"description\": \"docker\"}]}"
        ).ConfigureAwait(false);

        // Assert: Only doc1 has docker in all three fields
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    #endregion

    #region Cross-Format Equivalence Tests

    [Fact]
    public async Task InfixAndMongo_SameSemantics_ReturnSameResults()
    {
        // Arrange
        await this.InsertAsync("doc1", "kubernetes deployment").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker deployment").ConfigureAwait(false);
        await this.InsertAsync("doc3", "helm charts").ConfigureAwait(false);

        // Act: Same query in both formats
        var infixResponse = await this.SearchAsync("kubernetes OR docker").ConfigureAwait(false);
        var mongoResponse = await this.SearchAsync("{\"$or\": [{\"content\": \"kubernetes\"}, {\"content\": \"docker\"}]}").ConfigureAwait(false);

        // Assert: Both should return same results
        Assert.Equal(infixResponse.TotalResults, mongoResponse.TotalResults);
        Assert.Equal(infixResponse.Results.Length, mongoResponse.Results.Length);

        var infixIds = infixResponse.Results.Select(r => r.Id).OrderBy(x => x).ToArray();
        var mongoIds = mongoResponse.Results.Select(r => r.Id).OrderBy(x => x).ToArray();
        Assert.Equal(infixIds, mongoIds);
    }

    [Fact]
    public async Task InfixAndMongo_ComplexQuery_ReturnSameResults()
    {
        // Arrange
        await this.InsertAsync("doc1", "docker and kubernetes together").ConfigureAwait(false);
        await this.InsertAsync("doc2", "only docker here").ConfigureAwait(false);
        await this.InsertAsync("doc3", "only kubernetes here").ConfigureAwait(false);

        // Act: Complex AND query in both formats
        var infixResponse = await this.SearchAsync("docker AND kubernetes").ConfigureAwait(false);
        var mongoResponse = await this.SearchAsync("{\"$and\": [{\"content\": \"docker\"}, {\"content\": \"kubernetes\"}]}").ConfigureAwait(false);

        // Assert
        Assert.Equal(1, infixResponse.TotalResults);
        Assert.Equal(1, mongoResponse.TotalResults);
        Assert.Equal(infixResponse.Results[0].Id, mongoResponse.Results[0].Id);
        Assert.Equal(this._insertedIds["doc1"], infixResponse.Results[0].Id);
    }

    #endregion

    #region Pagination and Filtering Tests

    [Fact]
    public async Task Search_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange: Insert 10 documents
        for (int i = 0; i < 10; i++)
        {
            await this.InsertAsync($"doc{i}", $"test document number {i}").ConfigureAwait(false);
        }

        // Act: Get results with pagination
        var page1 = await this._searchService.SearchAsync(new SearchRequest
        {
            Query = "test",
            Limit = 3,
            Offset = 0,
            MinRelevance = 0.0f
        }, CancellationToken.None).ConfigureAwait(false);

        var page2 = await this._searchService.SearchAsync(new SearchRequest
        {
            Query = "test",
            Limit = 3,
            Offset = 3,
            MinRelevance = 0.0f
        }, CancellationToken.None).ConfigureAwait(false);

        // Assert: Pages should be different and non-overlapping
        Assert.Equal(3, page1.Results.Length);
        Assert.Equal(3, page2.Results.Length);

        var page1Ids = page1.Results.Select(r => r.Id).ToHashSet();
        var page2Ids = page2.Results.Select(r => r.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids)); // No overlap
    }

    [Fact]
    public async Task Search_TotalResults_ReflectsFilteredCountBeforePagination()
    {
        // Arrange: Insert 10 documents
        for (int i = 0; i < 10; i++)
        {
            await this.InsertAsync($"doc{i}", $"test item {i}").ConfigureAwait(false);
        }

        // Act: Search with limit=3
        var response = await this._searchService.SearchAsync(new SearchRequest
        {
            Query = "test",
            Limit = 3,
            MinRelevance = 0.0f
        }, CancellationToken.None).ConfigureAwait(false);

        // Assert: TotalResults should be 10 (total found), not 3 (paginated)
        Assert.Equal(10, response.TotalResults);
        Assert.Equal(3, response.Results.Length);
    }

    #endregion

    #region Regression Tests for Specific Bugs

    [Fact]
    public async Task RegressionTest_Bm25NormalizationBug_ScoresAboveMinRelevance()
    {
        // This test reproduces the critical bug that prevented all searches from working.
        // BM25 scores were ~0.000001, filtered out by MinRelevance=0.3

        // Arrange
        await this.InsertAsync("doc1", "simple test content").ConfigureAwait(false);

        // Act: Use default MinRelevance=0.3 (the value that exposed the bug)
        var response = await this.SearchAsync("test", minRelevance: 0.3f).ConfigureAwait(false);

        // Assert: Should find results (BM25 scores should be normalized to >= 0.3)
        Assert.True(response.TotalResults > 0, "BM25 scores not normalized - all results filtered out!");
        Assert.All(response.Results, r =>
        {
            Assert.True(r.Relevance >= 0.3f, $"Result has relevance {r.Relevance} < 0.3");
        });
    }

    [Fact]
    public async Task RegressionTest_FieldSpecificEqualOperator_ExtractsFtsQuery()
    {
        // This test reproduces the bug where "content:summaries" failed with SQLite error
        // because Equal operator wasn't extracting FTS queries

        // Arrange
        await this.InsertAsync("doc1", "summary of findings").ConfigureAwait(false);

        // Act: Field-specific query using : operator (maps to Equal)
        var response = await this.SearchAsync("content:summaries").ConfigureAwait(false);

        // Assert: Should find "summary" via stemming
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    #endregion

    #region Known Issue 2: Quoted Phrases With Reserved Words

    [Fact]
    public async Task KnownIssue2_QuotedPhraseWithAND_FindsExactPhrase()
    {
        // Known Issue 2: Quoted phrases don't escape operators
        // This test verifies that searching for "Alice AND Bob" (as a phrase)
        // finds documents containing that exact phrase, not documents with "Alice" AND "Bob" separately

        // Arrange
        await this.InsertAsync("doc1", "Meeting with Alice AND Bob").ConfigureAwait(false);
        await this.InsertAsync("doc2", "Alice went to lunch and Bob stayed").ConfigureAwait(false);
        await this.InsertAsync("doc3", "Just Alice here").ConfigureAwait(false);
        await this.InsertAsync("doc4", "Just Bob here").ConfigureAwait(false);

        // Act: Search for the exact phrase "Alice AND Bob" using quotes
        var response = await this.SearchAsync("\"Alice AND Bob\"").ConfigureAwait(false);

        // Assert: Should find only doc1 which contains the exact phrase
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_QuotedPhraseWithOR_FindsExactPhrase()
    {
        // Known Issue 2: Quoted phrases don't escape operators
        // This test verifies that "this OR that" searches for the literal phrase

        // Arrange
        await this.InsertAsync("doc1", "choose this OR that option").ConfigureAwait(false);
        await this.InsertAsync("doc2", "this is one option or that is another").ConfigureAwait(false);
        await this.InsertAsync("doc3", "just this").ConfigureAwait(false);
        await this.InsertAsync("doc4", "just that").ConfigureAwait(false);

        // Act: Search for the exact phrase "this OR that"
        var response = await this.SearchAsync("\"this OR that\"").ConfigureAwait(false);

        // Assert: Should find only doc1 with the exact phrase
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_QuotedPhraseWithNOT_FindsExactPhrase()
    {
        // Known Issue 2: Quoted phrases don't escape operators
        // This test verifies that "this is NOT important" searches for the literal phrase

        // Arrange
        await this.InsertAsync("doc1", "this is NOT important notice").ConfigureAwait(false);
        await this.InsertAsync("doc2", "this is definitely important").ConfigureAwait(false);
        await this.InsertAsync("doc3", "NOT a problem").ConfigureAwait(false);

        // Act: Search for the exact phrase "this is NOT important"
        var response = await this.SearchAsync("\"this is NOT important\"").ConfigureAwait(false);

        // Assert: Should find only doc1 with the exact phrase
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_QuotedReservedWordAND_FindsDocumentsContainingAND()
    {
        // Known Issue 2: Searching for just the word "AND" should work when quoted

        // Arrange
        await this.InsertAsync("doc1", "The word AND appears here").ConfigureAwait(false);
        await this.InsertAsync("doc2", "No reserved words").ConfigureAwait(false);

        // Act: Search for the literal word "AND"
        var response = await this.SearchAsync("\"AND\"").ConfigureAwait(false);

        // Assert: Should find doc1 containing "AND"
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_QuotedReservedWordOR_FindsDocumentsContainingOR()
    {
        // Known Issue 2: Searching for just the word "OR" should work when quoted

        // Arrange
        await this.InsertAsync("doc1", "The word OR appears here").ConfigureAwait(false);
        await this.InsertAsync("doc2", "No reserved words").ConfigureAwait(false);

        // Act: Search for the literal word "OR"
        var response = await this.SearchAsync("\"OR\"").ConfigureAwait(false);

        // Assert: Should find doc1 containing "OR"
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_QuotedReservedWordNOT_FindsDocumentsContainingNOT()
    {
        // Known Issue 2: Searching for just the word "NOT" should work when quoted

        // Arrange
        await this.InsertAsync("doc1", "The word NOT appears here").ConfigureAwait(false);
        await this.InsertAsync("doc2", "No reserved words").ConfigureAwait(false);

        // Act: Search for the literal word "NOT"
        var response = await this.SearchAsync("\"NOT\"").ConfigureAwait(false);

        // Assert: Should find doc1 containing "NOT"
        Assert.Equal(1, response.TotalResults);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue2_MixedQuotedPhraseAndOperator_WorksCorrectly()
    {
        // Known Issue 2: Mixing quoted phrases with actual operators should work
        // Search: "Alice AND Bob" AND kubernetes
        // This should find documents containing both the exact phrase "Alice AND Bob" AND the word "kubernetes"

        // Arrange
        await this.InsertAsync("doc1", "Meeting notes for Alice AND Bob about kubernetes").ConfigureAwait(false);
        await this.InsertAsync("doc2", "Meeting notes for Alice AND Bob about docker").ConfigureAwait(false);
        await this.InsertAsync("doc3", "Meeting notes for Alice about kubernetes").ConfigureAwait(false);

        // Act: Search for exact phrase AND another term
        var response = await this.SearchAsync("\"Alice AND Bob\" AND kubernetes").ConfigureAwait(false);

        // Assert: Should find only doc1
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc1"], response.Results[0].Id);
    }

    #endregion

    #region Known Issue 1: NOT Operator Fixes

    [Fact]
    public async Task KnownIssue1_StandaloneNOT_DoesNotCrash()
    {
        // Known Issue 1: Standalone NOT crashes with FTS5 syntax error
        // The query "NOT foo" should not throw an exception
        // Expected behavior: Return all documents that do NOT contain "foo"

        // Arrange
        await this.InsertAsync("doc1", "contains foo term").ConfigureAwait(false);
        await this.InsertAsync("doc2", "does not contain the term").ConfigureAwait(false);
        await this.InsertAsync("doc3", "another document without it").ConfigureAwait(false);

        // Act: This should NOT throw an exception
        var response = await this.SearchAsync("NOT foo").ConfigureAwait(false);

        // Assert: Should return documents that do NOT contain "foo"
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Contains "foo"
        Assert.Contains(this._insertedIds["doc2"], resultIds); // No "foo"
        Assert.Contains(this._insertedIds["doc3"], resultIds); // No "foo"
    }

    [Fact]
    public async Task KnownIssue1_NotWithPositiveTerm_ExcludesCorrectly()
    {
        // Known Issue 1: "foo AND NOT bar" should exclude documents with "bar"
        // Expected behavior: Return documents with "foo" but NOT "bar"
        // Note: Explicit AND is required - "foo NOT bar" is parsed as just "foo"

        // Arrange
        await this.InsertAsync("doc1", "foo and bar together").ConfigureAwait(false);
        await this.InsertAsync("doc2", "only foo here").ConfigureAwait(false);
        await this.InsertAsync("doc3", "only bar here").ConfigureAwait(false);
        await this.InsertAsync("doc4", "neither term here").ConfigureAwait(false);

        // Act: Search for "foo AND NOT bar" (explicit AND required)
        var response = await this.SearchAsync("foo AND NOT bar").ConfigureAwait(false);

        // Assert: Should return only doc2 (has "foo" but not "bar")
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc2"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue1_MultipleNOT_ExcludesAllTerms()
    {
        // Known Issue 1: Multiple NOT terms should all be excluded
        // Expected behavior: "foo AND NOT bar AND NOT baz" returns docs with "foo" but without "bar" and without "baz"
        // Note: Explicit AND is required between all terms

        // Arrange
        await this.InsertAsync("doc1", "foo bar baz all").ConfigureAwait(false);
        await this.InsertAsync("doc2", "foo bar only").ConfigureAwait(false);
        await this.InsertAsync("doc3", "foo baz only").ConfigureAwait(false);
        await this.InsertAsync("doc4", "foo alone here").ConfigureAwait(false);

        // Act: Search for foo but not bar and not baz (explicit AND required)
        var response = await this.SearchAsync("foo AND NOT bar AND NOT baz").ConfigureAwait(false);

        // Assert: Should return only doc4 (has "foo" without "bar" or "baz")
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc4"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue1_NOTWithOR_WorksCorrectly()
    {
        // Combined NOT with OR: "(foo OR baz) AND NOT bar"
        // Should return documents with "foo" OR "baz" but NOT "bar"
        // Note: Explicit AND is required between the OR group and NOT

        // Arrange
        await this.InsertAsync("doc1", "foo and bar").ConfigureAwait(false);
        await this.InsertAsync("doc2", "foo alone").ConfigureAwait(false);
        await this.InsertAsync("doc3", "baz and bar").ConfigureAwait(false);
        await this.InsertAsync("doc4", "baz alone").ConfigureAwait(false);
        await this.InsertAsync("doc5", "neither term").ConfigureAwait(false);

        // Act: Search for (foo OR baz) AND NOT bar (explicit AND required)
        var response = await this.SearchAsync("(foo OR baz) AND NOT bar").ConfigureAwait(false);

        // Assert: Should return doc2 and doc4 (have foo/baz but not bar)
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.Contains(this._insertedIds["doc4"], resultIds);
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Has bar
        Assert.DoesNotContain(this._insertedIds["doc3"], resultIds); // Has bar
    }

    [Fact]
    public async Task KnownIssue1_NOTInFieldQuery_ExcludesFromField()
    {
        // NOT with field-specific search: "content:foo AND NOT content:bar"
        // Should search in content field specifically
        // Note: Explicit AND is required

        // Arrange
        await this.InsertAsync("doc1", "foo bar in content", "title1").ConfigureAwait(false);
        await this.InsertAsync("doc2", "foo only in content", "title2").ConfigureAwait(false);
        await this.InsertAsync("doc3", "different content", "foo bar title").ConfigureAwait(false);

        // Act: Search for foo in content but not bar in content (explicit AND required)
        var response = await this.SearchAsync("content:foo AND NOT content:bar").ConfigureAwait(false);

        // Assert: Should return only doc2 (has foo in content, no bar in content)
        Assert.Equal(1, response.TotalResults);
        Assert.Single(response.Results);
        Assert.Equal(this._insertedIds["doc2"], response.Results[0].Id);
    }

    [Fact]
    public async Task KnownIssue1_MongoNot_ExcludesCorrectly()
    {
        // MongoDB $not operator should work correctly
        // $not: excludes documents matching the condition

        // Arrange
        await this.InsertAsync("doc1", "kubernetes deployment").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker deployment").ConfigureAwait(false);
        await this.InsertAsync("doc3", "other content").ConfigureAwait(false);

        // Act: MongoDB NOT - find documents NOT containing "kubernetes"
        var response = await this.SearchAsync("{\"$not\": {\"content\": \"kubernetes\"}}").ConfigureAwait(false);

        // Assert: Should return doc2 and doc3 (not containing kubernetes)
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Contains kubernetes
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.Contains(this._insertedIds["doc3"], resultIds);
    }

    [Fact]
    public async Task KnownIssue1_MongoNor_ExcludesAllConditions()
    {
        // MongoDB $nor operator should exclude all conditions

        // Arrange
        await this.InsertAsync("doc1", "kubernetes deployment").ConfigureAwait(false);
        await this.InsertAsync("doc2", "docker deployment").ConfigureAwait(false);
        await this.InsertAsync("doc3", "helm charts").ConfigureAwait(false);
        await this.InsertAsync("doc4", "ansible automation").ConfigureAwait(false);

        // Act: MongoDB NOR - find documents NOT containing kubernetes NOR docker
        var response = await this.SearchAsync("{\"$nor\": [{\"content\": \"kubernetes\"}, {\"content\": \"docker\"}]}").ConfigureAwait(false);

        // Assert: Should return doc3 and doc4 (not containing kubernetes or docker)
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Contains kubernetes
        Assert.DoesNotContain(this._insertedIds["doc2"], resultIds); // Contains docker
        Assert.Contains(this._insertedIds["doc3"], resultIds);
        Assert.Contains(this._insertedIds["doc4"], resultIds);
    }

    [Fact]
    public async Task KnownIssue1_NOTWithSingleQuotedReservedWord_ExcludesCorrectly()
    {
        // Single-quoted reserved words in NOT should be excluded correctly
        // Bug: NOT 'AND' was searching for literal "'AND'" instead of "AND"

        // Arrange
        await this.InsertAsync("doc1", "Meeting with Alice AND Bob tomorrow").ConfigureAwait(false);
        await this.InsertAsync("doc2", "Regular meeting notes").ConfigureAwait(false);
        await this.InsertAsync("doc3", "Project status update").ConfigureAwait(false);

        // Act: NOT with single-quoted AND (reserved word) - should exclude docs containing literal AND
        var response = await this.SearchAsync("NOT 'AND'").ConfigureAwait(false);

        // Assert: Should return doc2 and doc3 (not containing "AND")
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Contains "AND"
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.Contains(this._insertedIds["doc3"], resultIds);
    }

    [Fact]
    public async Task KnownIssue1_NOTWithDoubleQuotedReservedWord_ExcludesCorrectly()
    {
        // Double-quoted reserved words in NOT should be excluded correctly

        // Arrange
        await this.InsertAsync("doc1", "This is NOT important").ConfigureAwait(false);
        await this.InsertAsync("doc2", "Regular document content").ConfigureAwait(false);
        await this.InsertAsync("doc3", "Something else entirely").ConfigureAwait(false);

        // Act: NOT with double-quoted NOT (reserved word) - should exclude docs containing literal NOT
        var response = await this.SearchAsync("NOT \"NOT\"").ConfigureAwait(false);

        // Assert: Should return doc2 and doc3 (not containing "NOT")
        Assert.Equal(2, response.TotalResults);
        var resultIds = response.Results.Select(r => r.Id).ToHashSet();
        Assert.DoesNotContain(this._insertedIds["doc1"], resultIds); // Contains "NOT"
        Assert.Contains(this._insertedIds["doc2"], resultIds);
        Assert.Contains(this._insertedIds["doc3"], resultIds);
    }

    #endregion
}
