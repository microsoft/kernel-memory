// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings;
using KernelMemory.Core.Embeddings.Cache;
using Microsoft.Extensions.Logging;
using Moq;

namespace KernelMemory.Core.Tests.Embeddings;

/// <summary>
/// Tests for CachedEmbeddingGenerator decorator to verify cache hit/miss logic,
/// batch processing with mixed cache hits, and mode-based behavior.
/// </summary>
public sealed class CachedEmbeddingGeneratorTests
{
    private readonly Mock<IEmbeddingGenerator> _innerGeneratorMock;
    private readonly Mock<IEmbeddingCache> _cacheMock;
    private readonly Mock<ILogger<CachedEmbeddingGenerator>> _loggerMock;

    public CachedEmbeddingGeneratorTests()
    {
        this._innerGeneratorMock = new Mock<IEmbeddingGenerator>();
        this._cacheMock = new Mock<IEmbeddingCache>();
        this._loggerMock = new Mock<ILogger<CachedEmbeddingGenerator>>();

        // Setup default inner generator properties
        this._innerGeneratorMock.Setup(x => x.ProviderType).Returns(EmbeddingsTypes.OpenAI);
        this._innerGeneratorMock.Setup(x => x.ModelName).Returns("text-embedding-ada-002");
        this._innerGeneratorMock.Setup(x => x.VectorDimensions).Returns(1536);
        this._innerGeneratorMock.Setup(x => x.IsNormalized).Returns(true);
    }

    [Fact]
    public void Properties_ShouldDelegateToInnerGenerator()
    {
        // Arrange
        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act & Assert
        Assert.Equal(EmbeddingsTypes.OpenAI, cachedGenerator.ProviderType);
        Assert.Equal("text-embedding-ada-002", cachedGenerator.ModelName);
        Assert.Equal(1536, cachedGenerator.VectorDimensions);
        Assert.True(cachedGenerator.IsNormalized);
    }

    [Fact]
    public async Task GenerateAsync_Single_WithCacheHit_ShouldReturnCachedVector()
    {
        // Arrange
        var cachedVector = new float[] { 0.1f, 0.2f, 0.3f };
        var cachedEmbedding = new CachedEmbedding
        {
            Vector = cachedVector
        };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEmbedding);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var result = await cachedGenerator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(cachedVector, result);
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_Single_WithCacheMiss_ShouldCallInnerGenerator()
    {
        // Arrange
        var generatedVector = new float[] { 0.4f, 0.5f, 0.6f };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVector);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var result = await cachedGenerator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(generatedVector, result);
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync("test text", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_Single_WithCacheMiss_ShouldStoreInCache()
    {
        // Arrange
        var generatedVector = new float[] { 0.4f, 0.5f, 0.6f };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVector);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        await cachedGenerator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        this._cacheMock.Verify(
            x => x.StoreAsync(It.IsAny<EmbeddingCacheKey>(), generatedVector, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_Single_WithWriteOnlyCache_ShouldSkipCacheRead()
    {
        // Arrange
        var generatedVector = new float[] { 0.4f, 0.5f, 0.6f };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.WriteOnly);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVector);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var result = await cachedGenerator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(generatedVector, result);
        this._cacheMock.Verify(
            x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()),
            Times.Never);
        this._cacheMock.Verify(
            x => x.StoreAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<float[]>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_Single_WithReadOnlyCache_ShouldSkipCacheWrite()
    {
        // Arrange
        var generatedVector = new float[] { 0.4f, 0.5f, 0.6f };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadOnly);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVector);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var result = await cachedGenerator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(generatedVector, result);
        this._cacheMock.Verify(
            x => x.StoreAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<float[]>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_Batch_AllCacheHits_ShouldNotCallInnerGenerator()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var cachedVectors = new Dictionary<string, float[]>
        {
            ["text1"] = new[] { 0.1f, 0.2f },
            ["text2"] = new[] { 0.3f, 0.4f },
            ["text3"] = new[] { 0.5f, 0.6f }
        };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmbeddingCacheKey key, CancellationToken ct) =>
            {
                // Find the matching text by checking the hash
                foreach (var kvp in cachedVectors)
                {
                    var testKey = EmbeddingCacheKey.Create("OpenAI", "text-embedding-ada-002", 1536, true, kvp.Key);
                    if (testKey.TextHash == key.TextHash)
                    {
                        return new CachedEmbedding { Vector = kvp.Value };
                    }
                }

                return null;
            });

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var results = await cachedGenerator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_Batch_AllCacheMisses_ShouldCallInnerGeneratorWithAllTexts()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var generatedVectors = new[]
        {
            new[] { 0.1f, 0.2f },
            new[] { 0.3f, 0.4f },
            new[] { 0.5f, 0.6f }
        };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVectors);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var results = await cachedGenerator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync(It.Is<IEnumerable<string>>(t => t.Count() == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_Batch_MixedHitsAndMisses_ShouldOnlyGenerateMisses()
    {
        // Arrange
        var texts = new[] { "cached", "not-cached-1", "not-cached-2" };
        var cachedVector = new[] { 0.1f, 0.2f };
        var generatedVectors = new[]
        {
            new[] { 0.3f, 0.4f },
            new[] { 0.5f, 0.6f }
        };

        var cachedKey = EmbeddingCacheKey.Create("OpenAI", "text-embedding-ada-002", 1536, true, "cached");

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmbeddingCacheKey key, CancellationToken ct) =>
            {
                if (key.TextHash == cachedKey.TextHash)
                {
                    return new CachedEmbedding { Vector = cachedVector };
                }

                return null;
            });

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVectors);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var results = await cachedGenerator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        // First result should be cached
        Assert.Equal(cachedVector, results[0]);
        // Other results should be generated
        Assert.Equal(generatedVectors[0], results[1]);
        Assert.Equal(generatedVectors[1], results[2]);

        // Verify only non-cached texts were sent to generator
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync(It.Is<IEnumerable<string>>(t => t.Count() == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldStoreGeneratedInCache()
    {
        // Arrange
        var texts = new[] { "text1", "text2" };
        var generatedVectors = new[]
        {
            new[] { 0.1f, 0.2f },
            new[] { 0.3f, 0.4f }
        };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVectors);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        await cachedGenerator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert - Both generated vectors should be stored
        this._cacheMock.Verify(
            x => x.StoreAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<float[]>(), null, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_Batch_EmptyInput_ShouldReturnEmptyArray()
    {
        // Arrange
        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var results = await cachedGenerator.GenerateAsync(Array.Empty<string>(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Empty(results);
        this._innerGeneratorMock.Verify(
            x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldPreserveOrder()
    {
        // Arrange - Specifically test that results maintain input order
        var texts = new[] { "a", "b", "c", "d" };

        // Make only "b" and "d" cached
        var cachedB = EmbeddingCacheKey.Create("OpenAI", "text-embedding-ada-002", 1536, true, "b");
        var cachedD = EmbeddingCacheKey.Create("OpenAI", "text-embedding-ada-002", 1536, true, "d");
        var vectorB = new[] { 2.0f };
        var vectorD = new[] { 4.0f };

        var generatedVectors = new[]
        {
            new[] { 1.0f }, // for "a"
            new[] { 3.0f }  // for "c"
        };

        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmbeddingCacheKey key, CancellationToken ct) =>
            {
                if (key.TextHash == cachedB.TextHash)
                {
                    return new CachedEmbedding { Vector = vectorB };
                }

                if (key.TextHash == cachedD.TextHash)
                {
                    return new CachedEmbedding { Vector = vectorD };
                }

                return null;
            });

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedVectors);

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        // Act
        var results = await cachedGenerator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert - Order must be preserved: a, b, c, d
        Assert.Equal(4, results.Length);
        Assert.Equal(new[] { 1.0f }, results[0]); // a - generated
        Assert.Equal(new[] { 2.0f }, results[1]); // b - cached
        Assert.Equal(new[] { 3.0f }, results[2]); // c - generated
        Assert.Equal(new[] { 4.0f }, results[3]); // d - cached
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_ShouldPropagate()
    {
        // Arrange
        this._cacheMock.Setup(x => x.Mode).Returns(CacheModes.ReadWrite);
        this._cacheMock
            .Setup(x => x.TryGetAsync(It.IsAny<EmbeddingCacheKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedEmbedding?)null);

        this._innerGeneratorMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var cachedGenerator = new CachedEmbeddingGenerator(
            this._innerGeneratorMock.Object,
            this._cacheMock.Object,
            this._loggerMock.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => cachedGenerator.GenerateAsync("test", cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public void Constructor_WithNullInnerGenerator_ShouldThrow()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedEmbeddingGenerator(null!, this._cacheMock.Object, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrow()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedEmbeddingGenerator(this._innerGeneratorMock.Object, null!, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedEmbeddingGenerator(this._innerGeneratorMock.Object, this._cacheMock.Object, null!));
    }
}
