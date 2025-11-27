using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Core.Tests.Storage;

/// <summary>
/// Integration tests for ContentStorageService using real SQLite database files.
/// Tests the full stack including database schema, migrations, and persistence.
/// </summary>
public sealed class ContentStorageIntegrationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly ContentStorageDbContext _context;
    private readonly ContentStorageService _service;
    private readonly Mock<ILogger<ContentStorageService>> _mockLogger;

    public ContentStorageIntegrationTests()
    {
        // Use temporary SQLite file for integration tests
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_km_{Guid.NewGuid()}.db");

        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        _context = new ContentStorageDbContext(options);
        _context.Database.EnsureCreated();

        _mockLogger = new Mock<ILogger<ContentStorageService>>();

        // Use real CuidGenerator for integration tests
        var cuidGenerator = new CuidGenerator();
        _service = new ContentStorageService(_context, cuidGenerator, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();

        // Clean up temporary database file
        if (File.Exists(_tempDbPath))
        {
            File.Delete(_tempDbPath);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DatabaseSchema_IsCreatedCorrectlyAsync()
    {
        // Assert - Verify tables exist
        var tables = await _context.Database.SqlQueryRaw<string>(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
            .ToListAsync().ConfigureAwait(false);

        Assert.Contains("km_content", tables);
        Assert.Contains("km_operations", tables);
    }

    [Fact]
    public async Task ContentTable_HasCorrectIndexesAsync()
    {
        // Assert - Verify indexes on Content table
        var indexes = await _context.Database.SqlQueryRaw<string>(
            "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='km_content'")
            .ToListAsync().ConfigureAwait(false);

        Assert.Contains(indexes, idx => idx.Contains("Ready"));
    }

    [Fact]
    public async Task OperationsTable_HasCorrectIndexesAsync()
    {
        // Assert - Verify indexes on Operations table
        var indexes = await _context.Database.SqlQueryRaw<string>(
            "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='km_operations'")
            .ToListAsync().ConfigureAwait(false);

        Assert.Contains(indexes, idx => idx.Contains("ContentId"));
        Assert.Contains(indexes, idx => idx.Contains("Complete"));
        Assert.Contains(indexes, idx => idx.Contains("Timestamp"));
    }

    [Fact]
    public async Task FullWorkflow_UpsertRetrieveDeleteAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Integration test content",
            MimeType = "text/plain",
            Title = "Integration Test",
            Description = "Testing full workflow",
            Tags = ["integration", "test"],
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "integration_test",
                ["version"] = "1.0"
            }
        };

        // Act 1: Upsert
        var contentId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert 1: Content exists
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Integration test content", content.Content);
        Assert.Equal("text/plain", content.MimeType);
        Assert.Equal("Integration Test", content.Title);
        Assert.Equal("Testing full workflow", content.Description);
        Assert.Equal(2, content.Tags.Length);
        Assert.Equal(2, content.Metadata.Count);

        // Act 2: Update
        var updateRequest = new UpsertRequest
        {
            Id = contentId,
            Content = "Updated content",
            MimeType = "text/html",
            Title = "Updated Title"
        };
        await _service.UpsertAsync(updateRequest).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert 2: Content is updated
        var updatedContent = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(updatedContent);
        Assert.Equal("Updated content", updatedContent.Content);
        Assert.Equal("text/html", updatedContent.MimeType);
        Assert.Equal("Updated Title", updatedContent.Title);

        // Act 3: Delete
        await _service.DeleteAsync(contentId).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert 3: Content is deleted
        var deletedContent = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.Null(deletedContent);
    }

    [Fact]
    public async Task RealCuidGenerator_GeneratesValidIdsAsync()
    {
        // Act - Create multiple content items with real CUID generation
        var ids = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var id = await _service.UpsertAsync(new UpsertRequest
            {
                Content = $"Content {i}",
                MimeType = "text/plain"
            }).ConfigureAwait(false);
            ids.Add(id);
        }

        // Assert - All IDs should be unique and non-empty
        Assert.Equal(10, ids.Distinct().Count());
        Assert.All(ids, id =>
        {
            Assert.NotEmpty(id);
            Assert.True(id.Length >= 20); // CUIDs are typically 25-32 chars
        });
    }

    [Fact]
    public async Task Persistence_SurvivesDatabaseReopenAsync()
    {
        // Arrange - Create content
        var request = new UpsertRequest
        {
            Id = "persistent_test",
            Content = "Persistent content",
            MimeType = "text/plain",
            Title = "Persistence Test"
        };

        await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Act - Dispose and recreate context (simulates app restart)
        await _context.DisposeAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite($"Data Source={_tempDbPath}")
            .Options;

        using var newContext = new ContentStorageDbContext(options);
        var newService = new ContentStorageService(
            newContext,
            new CuidGenerator(),
            _mockLogger.Object);

        // Assert - Content should still exist
        var content = await newService.GetByIdAsync("persistent_test").ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Persistent content", content.Content);
        Assert.Equal("Persistence Test", content.Title);
    }

    [Fact]
    public async Task MultipleOperations_ProcessInOrderAsync()
    {
        // Arrange
        var contentId = "ordered_test";
        var operations = new List<string>();

        // Act - Create multiple operations quickly
        for (int i = 1; i <= 5; i++)
        {
            await _service.UpsertAsync(new UpsertRequest
            {
                Id = contentId,
                Content = $"Version {i}",
                MimeType = "text/plain"
            }).ConfigureAwait(false);
            await Task.Delay(10).ConfigureAwait(false); // Small delay to ensure timestamp order
        }

        // Wait for all operations to process
        await Task.Delay(500).ConfigureAwait(false);

        // Assert - Final content should be the last version
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Version 5", content.Content);
    }

    [Fact]
    public async Task OperationQueue_HandlesFailureGracefullyAsync()
    {
        // This test verifies that operations are queued even if processing might fail
        // Arrange
        var contentId = "failure_test";

        // Act - Queue operation
        await _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Test content",
            MimeType = "text/plain"
        }).ConfigureAwait(false);

        // Assert - Operation should be queued (Phase 1 always succeeds)
        var operation = await _context.Operations
            .FirstOrDefaultAsync(o => o.ContentId == contentId).ConfigureAwait(false);

        Assert.NotNull(operation);
        Assert.False(operation.Complete || operation.Cancelled);
    }

    [Fact]
    public async Task DateTimeOffset_IsStoredAndRetrievedCorrectlyAsync()
    {
        // Arrange - Use specific timestamp
        var specificDate = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(-7));
        var request = new UpsertRequest
        {
            Content = "Date test content",
            MimeType = "text/plain",
            ContentCreatedAt = specificDate
        };

        // Act
        var contentId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(specificDate, content.ContentCreatedAt);
    }

    [Fact]
    public async Task JsonSerialization_HandlesComplexMetadataAsync()
    {
        // Arrange - Complex metadata with special characters
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain",
            Tags = ["tag with spaces", "tag-with-dashes", "tag_with_underscores"],
            Metadata = new Dictionary<string, string>
            {
                ["key with spaces"] = "value with spaces",
                ["special-chars"] = "value!@#$%^&*()",
                ["unicode"] = "你好世界🌍",
                ["json-like"] = "{\"nested\": \"value\"}"
            }
        };

        // Act
        var contentId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(3, content.Tags.Length);
        Assert.Equal("tag with spaces", content.Tags[0]);
        Assert.Equal(4, content.Metadata.Count);
        Assert.Equal("你好世界🌍", content.Metadata["unicode"]);
        Assert.Equal("{\"nested\": \"value\"}", content.Metadata["json-like"]);
    }

    [Fact]
    public async Task CountAsync_ReflectsDatabaseStateAsync()
    {
        // Arrange - Initial count
        var initialCount = await _service.CountAsync().ConfigureAwait(false);

        // Act - Add 3 items
        for (int i = 0; i < 3; i++)
        {
            await _service.UpsertAsync(new UpsertRequest
            {
                Content = $"Content {i}",
                MimeType = "text/plain"
            }).ConfigureAwait(false);
        }
        await Task.Delay(300).ConfigureAwait(false); // Wait for processing

        // Assert - Count increased by 3
        var afterAddCount = await _service.CountAsync().ConfigureAwait(false);
        Assert.Equal(initialCount + 3, afterAddCount);

        // Act - Delete 1 item
        var content = await _context.Content.FirstAsync().ConfigureAwait(false);
        await _service.DeleteAsync(content.Id).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert - Count decreased by 1
        var afterDeleteCount = await _service.CountAsync().ConfigureAwait(false);
        Assert.Equal(afterAddCount - 1, afterDeleteCount);
    }

    [Fact]
    public async Task EmptyStringFields_AreHandledCorrectlyAsync()
    {
        // Arrange - Use empty strings for optional fields
        var request = new UpsertRequest
        {
            Content = "Content only",
            MimeType = "text/plain",
            Title = string.Empty,
            Description = string.Empty,
            Tags = [],
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var contentId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(200).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(string.Empty, content.Title);
        Assert.Equal(string.Empty, content.Description);
        Assert.Empty(content.Tags);
        Assert.Empty(content.Metadata);
    }

    [Fact]
    public async Task ConcurrentWrites_ToSameContent_AreSerializedCorrectlyAsync()
    {
        // Arrange
        var contentId = "concurrent_integration_test";

        // Act - Fire multiple concurrent upserts
        var tasks = Enumerable.Range(1, 10).Select(i =>
            _service.UpsertAsync(new UpsertRequest
            {
                Id = contentId,
                Content = $"Concurrent Version {i}",
                MimeType = "text/plain"
            })).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false); // Wait for all operations to process

        // Assert - Should have exactly one content record (last one wins)
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.StartsWith("Concurrent Version", content.Content);

        // Verify only one content record exists with this ID
        var count = await _context.Content.CountAsync(c => c.Id == contentId).ConfigureAwait(false);
        Assert.Equal(1, count);
    }
}
