// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Search;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Search;

/// <summary>
/// Tests that verify FTS index data persistence across dispose/create cycles.
/// This reproduces the CLI scenario where put and search use different service instances.
/// </summary>
public sealed class FtsIndexPersistenceTest : IDisposable
{
    private readonly string _tempDir;
    private readonly string _contentDbPath;
    private readonly string _ftsDbPath;

    public FtsIndexPersistenceTest()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-fts-persist-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);
        this._contentDbPath = Path.Combine(this._tempDir, "content.db");
        this._ftsDbPath = Path.Combine(this._tempDir, "fts.db");
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
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task IndexThenDisposeThenSearch_ShouldFindIndexedContent()
    {
        // Arrange: Create services, index content, then dispose (simulating put command)
        string contentId;
        {
            var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();
            using var ftsIndex = new SqliteFtsIndex(this._ftsDbPath, enableStemming: true, mockLogger.Object);

            contentId = "test-id-123";
            await ftsIndex.IndexAsync(contentId, "Test Title", "Test Description", "hello world").ConfigureAwait(false);

            // Dispose should checkpoint and persist data
        }

        // Act: Create NEW FTS index instance and search (simulating search command)
        {
            var mockLogger = new Mock<ILogger<SqliteFtsIndex>>();
            using var ftsIndex = new SqliteFtsIndex(this._ftsDbPath, enableStemming: true, mockLogger.Object);

            var results = await ftsIndex.SearchAsync("hello", 10).ConfigureAwait(false);

            // Assert: Should find the content indexed in the previous instance
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ContentId == contentId);
        }
    }
}
