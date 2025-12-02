// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings.Cache;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings;

/// <summary>
/// Decorator that wraps an IEmbeddingGenerator with caching support.
/// Checks the cache before generating embeddings, and stores new embeddings in the cache.
/// Supports different cache modes (ReadWrite, ReadOnly, WriteOnly).
/// </summary>
public sealed class CachedEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly IEmbeddingGenerator _inner;
    private readonly IEmbeddingCache _cache;
    private readonly ILogger<CachedEmbeddingGenerator> _logger;

    /// <inheritdoc />
    public EmbeddingsTypes ProviderType => this._inner.ProviderType;

    /// <inheritdoc />
    public string ModelName => this._inner.ModelName;

    /// <inheritdoc />
    public int VectorDimensions => this._inner.VectorDimensions;

    /// <inheritdoc />
    public bool IsNormalized => this._inner.IsNormalized;

    /// <summary>
    /// Creates a new cached embedding generator decorator.
    /// </summary>
    /// <param name="inner">The inner generator to wrap.</param>
    /// <param name="cache">The cache to use for storing/retrieving embeddings.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">When inner, cache, or logger is null.</exception>
    public CachedEmbeddingGenerator(
        IEmbeddingGenerator inner,
        IEmbeddingCache cache,
        ILogger<CachedEmbeddingGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner, nameof(inner));
        ArgumentNullException.ThrowIfNull(cache, nameof(cache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this._inner = inner;
        this._cache = cache;
        this._logger = logger;

        this._logger.LogDebug(
            "CachedEmbeddingGenerator initialized for {Provider}/{Model} with cache mode {Mode}",
            inner.ProviderType, inner.ModelName, cache.Mode);
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        var key = this.BuildCacheKey(text);

        // Try cache read (if mode allows)
        if (this._cache.Mode != CacheModes.WriteOnly)
        {
            var cached = await this._cache.TryGetAsync(key, ct).ConfigureAwait(false);
            if (cached != null)
            {
                this._logger.LogDebug("Cache hit for single embedding, dimensions: {Dimensions}", cached.Vector.Length);
                return cached.Vector;
            }
        }

        // Generate embedding
        this._logger.LogDebug("Cache miss for single embedding, calling {Provider}", this.ProviderType);
        var vector = await this._inner.GenerateAsync(text, ct).ConfigureAwait(false);

        // Store in cache (if mode allows)
        if (this._cache.Mode != CacheModes.ReadOnly)
        {
            await this._cache.StoreAsync(key, vector, ct).ConfigureAwait(false);
            this._logger.LogDebug("Stored embedding in cache, dimensions: {Dimensions}", vector.Length);
        }

        return vector;
    }

    /// <inheritdoc />
    public async Task<float[][]> GenerateAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        // Initialize result array with nulls
        var results = new float[textList.Count][];

        // Track which texts need to be generated
        var toGenerate = new List<(int Index, string Text)>();

        // Try cache reads (if mode allows)
        if (this._cache.Mode != CacheModes.WriteOnly)
        {
            for (int i = 0; i < textList.Count; i++)
            {
                var key = this.BuildCacheKey(textList[i]);
                var cached = await this._cache.TryGetAsync(key, ct).ConfigureAwait(false);

                if (cached != null)
                {
                    results[i] = cached.Vector;
                }
                else
                {
                    toGenerate.Add((i, textList[i]));
                }
            }

            this._logger.LogDebug(
                "Batch cache lookup: {HitCount} hits, {MissCount} misses",
                textList.Count - toGenerate.Count, toGenerate.Count);
        }
        else
        {
            // WriteOnly mode - all texts need to be generated
            for (int i = 0; i < textList.Count; i++)
            {
                toGenerate.Add((i, textList[i]));
            }
        }

        // Generate missing embeddings
        if (toGenerate.Count > 0)
        {
            var textsToGenerate = toGenerate.Select(x => x.Text);
            var generatedVectors = await this._inner.GenerateAsync(textsToGenerate, ct).ConfigureAwait(false);

            // Map generated vectors back to results and store in cache
            for (int i = 0; i < toGenerate.Count; i++)
            {
                var (originalIndex, text) = toGenerate[i];
                results[originalIndex] = generatedVectors[i];

                // Store in cache (if mode allows)
                if (this._cache.Mode != CacheModes.ReadOnly)
                {
                    var key = this.BuildCacheKey(text);
                    await this._cache.StoreAsync(key, generatedVectors[i], ct).ConfigureAwait(false);
                }
            }

            this._logger.LogDebug("Generated and cached {Count} embeddings", toGenerate.Count);
        }

        return results;
    }

    /// <summary>
    /// Builds a cache key for the given text using the inner generator's properties.
    /// </summary>
    private EmbeddingCacheKey BuildCacheKey(string text)
    {
        return EmbeddingCacheKey.Create(
            provider: this._inner.ProviderType.ToString(),
            model: this._inner.ModelName,
            vectorDimensions: this._inner.VectorDimensions,
            isNormalized: this._inner.IsNormalized,
            text: text);
    }
}
