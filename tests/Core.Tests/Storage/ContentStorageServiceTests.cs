using KernelMemory.Core.Storage;
using KernelMemory.Core.Storage.Entities;
using KernelMemory.Core.Storage.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Core.Tests.Storage;

/// <summary>
/// Unit tests for ContentStorageService using in-memory SQLite database.
/// Tests cover the queue-based execution model and two-phase write pattern.
/// </summary>
public sealed class ContentStorageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ContentStorageDbContext _context;
    private readonly Mock<ICuidGenerator> _mockCuidGenerator;
    private readonly Mock<ILogger<ContentStorageService>> _mockLogger;
    private readonly ContentStorageService _service;
    private int _cuidCounter;

    public ContentStorageServiceTests()
    {
        // Use in-memory SQLite for fast isolated tests
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ContentStorageDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ContentStorageDbContext(options);
        _context.Database.EnsureCreated();

        // Mock CUID generator with predictable IDs
        _mockCuidGenerator = new Mock<ICuidGenerator>();
        _cuidCounter = 0;
        _mockCuidGenerator
            .Setup(x => x.Generate())
            .Returns(() => $"test_id_{++_cuidCounter:D5}");

        _mockLogger = new Mock<ILogger<ContentStorageService>>();

        _service = new ContentStorageService(_context, _mockCuidGenerator.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UpsertAsync_WithEmptyId_GeneratesNewIdAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain",
            Title = "Test"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);

        // Assert
        Assert.Equal("test_id_00001", resultId); // First generated ID

        // Verify content was created
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Test content", content.Content);
        Assert.Equal("text/plain", content.MimeType);
        Assert.Equal("Test", content.Title);
    }

    [Fact]
    public async Task UpsertAsync_WithProvidedId_UsesProvidedIdAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Id = "custom_id_123",
            Content = "Test content",
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);

        // Assert
        Assert.Equal("custom_id_123", resultId);

        // Verify content was created
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Test content", content.Content);
    }

    [Fact]
    public async Task UpsertAsync_ReplacesExistingContentAsync()
    {
        // Arrange - Create initial content
        var initialRequest = new UpsertRequest
        {
            Id = "test_id_replace",
            Content = "Initial content",
            MimeType = "text/plain",
            Title = "Initial Title"
        };
        await _service.UpsertAsync(initialRequest).ConfigureAwait(false);

        // Wait for processing to complete
        await Task.Delay(100).ConfigureAwait(false);

        // Act - Replace with new content
        var replaceRequest = new UpsertRequest
        {
            Id = "test_id_replace",
            Content = "Replaced content",
            MimeType = "text/html",
            Title = "New Title"
        };
        await _service.UpsertAsync(replaceRequest).ConfigureAwait(false);

        // Wait for processing to complete
        await Task.Delay(100).ConfigureAwait(false);

        // Assert
        var content = await _service.GetByIdAsync("test_id_replace").ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Replaced content", content.Content);
        Assert.Equal("text/html", content.MimeType);
        Assert.Equal("New Title", content.Title);
    }

    [Fact]
    public async Task UpsertAsync_StoresTagsAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain",
            Tags = ["tag1", "tag2", "tag3"]
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(3, content.Tags.Length);
        Assert.Contains("tag1", content.Tags);
        Assert.Contains("tag2", content.Tags);
        Assert.Contains("tag3", content.Tags);
    }

    [Fact]
    public async Task UpsertAsync_StoresMetadataAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain",
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(2, content.Metadata.Count);
        Assert.Equal("value1", content.Metadata["key1"]);
        Assert.Equal("value2", content.Metadata["key2"]);
    }

    [Fact]
    public async Task UpsertAsync_CalculatesByteSizeAsync()
    {
        // Arrange
        var testContent = "Test content with some length";
        var request = new UpsertRequest
        {
            Content = testContent,
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(testContent), content.ByteSize);
    }

    [Fact]
    public async Task UpsertAsync_UsesCustomContentCreatedAtAsync()
    {
        // Arrange
        var customDate = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain",
            ContentCreatedAt = customDate
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(customDate, content.ContentCreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingContentAsync()
    {
        // Arrange - Create content first
        var request = new UpsertRequest
        {
            Id = "test_id_delete",
            Content = "Content to delete",
            MimeType = "text/plain"
        };
        await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Verify content exists
        var contentBefore = await _service.GetByIdAsync("test_id_delete").ConfigureAwait(false);
        Assert.NotNull(contentBefore);

        // Act - Delete the content
        await _service.DeleteAsync("test_id_delete").ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert - Content should be gone
        var contentAfter = await _service.GetByIdAsync("test_id_delete").ConfigureAwait(false);
        Assert.Null(contentAfter);
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotentAsync()
    {
        // Act - Delete non-existent content (should not throw)
        await _service.DeleteAsync("non_existent_id").ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert - No exception thrown, verify content doesn't exist
        var content = await _service.GetByIdAsync("non_existent_id").ConfigureAwait(false);
        Assert.Null(content);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistentAsync()
    {
        // Act
        var content = await _service.GetByIdAsync("non_existent_id").ConfigureAwait(false);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCountAsync()
    {
        // Arrange - Create multiple content records
        for (int i = 0; i < 5; i++)
        {
            await _service.UpsertAsync(new UpsertRequest
            {
                Content = $"Content {i}",
                MimeType = "text/plain"
            }).ConfigureAwait(false);
        }
        await Task.Delay(500).ConfigureAwait(false); // Wait for all to process

        // Act
        var count = await _service.CountAsync().ConfigureAwait(false);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task UpsertAsync_QueuesOperationSuccessfullyAsync()
    {
        // Arrange
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);

        // Assert - Operation should be queued
        var operation = await _context.Operations
            .FirstOrDefaultAsync(o => o.ContentId == resultId).ConfigureAwait(false);

        Assert.NotNull(operation);
        Assert.False(operation.Complete);
        Assert.False(operation.Cancelled);
        Assert.Contains("upsert", operation.PlannedSteps);
    }

    [Fact]
    public async Task DeleteAsync_QueuesOperationSuccessfullyAsync()
    {
        // Arrange
        var contentId = "test_delete_queue";

        // Act
        await _service.DeleteAsync(contentId).ConfigureAwait(false);

        // Assert - Operation should be queued
        var operation = await _context.Operations
            .FirstOrDefaultAsync(o => o.ContentId == contentId).ConfigureAwait(false);

        Assert.NotNull(operation);
        Assert.False(operation.Complete);
        Assert.False(operation.Cancelled);
        Assert.Contains("delete", operation.PlannedSteps);
    }

    [Fact]
    public async Task ConcurrentUpserts_LastOneWinsAsync()
    {
        // Arrange
        var contentId = "concurrent_test";

        // Act - Simulate concurrent upserts
        var task1 = _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 1",
            MimeType = "text/plain"
        });

        var task2 = _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 2",
            MimeType = "text/plain"
        });

        var task3 = _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 3",
            MimeType = "text/plain"
        });

        await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
        await Task.Delay(300).ConfigureAwait(false); // Wait for all operations to process

        // Assert - Last version should win
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Version 3", content.Content); // Latest should win
    }

    [Fact]
    public async Task OperationCancellation_SupersededUpsertsAsync()
    {
        // Arrange
        var contentId = "cancellation_test";

        // Act - Create multiple upsert operations
        await _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 1",
            MimeType = "text/plain"
        }).ConfigureAwait(false);

        await _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 2",
            MimeType = "text/plain"
        }).ConfigureAwait(false);

        await Task.Delay(500).ConfigureAwait(false); // Wait for processing

        // Assert - Verify operations were queued (Phase 1 always succeeds)
        var operations = await _context.Operations
            .Where(o => o.ContentId == contentId)
            .OrderBy(o => o.Timestamp)
            .ToListAsync().ConfigureAwait(false);

        Assert.Equal(2, operations.Count);

        // Eventually, the final content should be Version 2 (last write wins)
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal("Version 2", content.Content);
    }

    [Fact]
    public async Task Delete_CancelsAllPreviousOperationsAsync()
    {
        // Arrange
        var contentId = "delete_cancellation_test";

        // Create multiple upsert operations
        await _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 1",
            MimeType = "text/plain"
        }).ConfigureAwait(false);

        await _service.UpsertAsync(new UpsertRequest
        {
            Id = contentId,
            Content = "Version 2",
            MimeType = "text/plain"
        }).ConfigureAwait(false);

        // Act - Delete should queue a delete operation and try to cancel previous ops
        await _service.DeleteAsync(contentId).ConfigureAwait(false);
        await Task.Delay(500).ConfigureAwait(false); // Wait for processing

        // Assert - Delete operation was queued (Phase 1 always succeeds)
        var deleteOps = await _context.Operations
            .Where(o => o.ContentId == contentId && o.PlannedStepsJson.Contains("delete"))
            .ToListAsync().ConfigureAwait(false);

        Assert.NotEmpty(deleteOps);

        // Eventually, content should be deleted (delete is the last operation)
        var content = await _service.GetByIdAsync(contentId).ConfigureAwait(false);
        Assert.Null(content);
    }

    [Fact]
    public async Task RecordTimestamps_AreSetCorrectlyAsync()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow.AddSeconds(-1);
        var request = new UpsertRequest
        {
            Content = "Test content",
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing
        var afterCreate = DateTimeOffset.UtcNow.AddSeconds(1);

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.InRange(content.RecordCreatedAt, beforeCreate, afterCreate);
        Assert.InRange(content.RecordUpdatedAt, beforeCreate, afterCreate);
    }

    [Fact]
    public async Task EmptyContent_IsAllowedAsync()
    {
        // Arrange - Empty content should be allowed
        var request = new UpsertRequest
        {
            Content = string.Empty,
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false); // Wait for processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(string.Empty, content.Content);
        Assert.Equal(0, content.ByteSize);
    }

    [Fact]
    public async Task UpsertAsync_HandlesLargeContentAsync()
    {
        // Arrange - Create large content (1MB)
        var largeContent = new string('x', 1024 * 1024);
        var request = new UpsertRequest
        {
            Content = largeContent,
            MimeType = "text/plain"
        };

        // Act
        var resultId = await _service.UpsertAsync(request).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false); // Wait longer for large content processing

        // Assert
        var content = await _service.GetByIdAsync(resultId).ConfigureAwait(false);
        Assert.NotNull(content);
        Assert.Equal(largeContent.Length, content.Content.Length);
        Assert.True(content.ByteSize >= 1024 * 1024); // Should be at least 1MB (UTF-8 encoding)
    }
}
