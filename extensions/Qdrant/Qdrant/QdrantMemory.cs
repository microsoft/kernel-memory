// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryDb.Qdrant.Client;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant;

/// <summary>
/// Qdrant connector for Kernel Memory
/// TODO:
/// * allow using more Qdrant specific filtering logic
/// </summary>
[Experimental("KMEXP03")]
public sealed class QdrantMemory : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly QdrantClient<DefaultQdrantPayload> _qdrantClient;
    private readonly ILogger<QdrantMemory> _log;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Qdrant connector configuration</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public QdrantMemory(
        QdrantConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<QdrantMemory>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;

        if (this._embeddingGenerator == null)
        {
            throw new QdrantException("Embedding generator not configured");
        }

        this._log = log ?? DefaultLogger<QdrantMemory>.Instance;
        this._qdrantClient = new QdrantClient<DefaultQdrantPayload>(endpoint: config.Endpoint, apiKey: config.APIKey);
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(
        string index, int vectorSize,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        return this._qdrantClient.CreateCollectionAsync(index, vectorSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        return await this._qdrantClient
            .GetCollectionsAsync(cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        try
        {
            index = NormalizeIndexName(index);
            return this._qdrantClient.DeleteCollectionAsync(index, cancellationToken);
        }
        catch (IndexNotFoundException)
        {
            this._log.LogInformation("Index not found, nothing to delete");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        QdrantPoint<DefaultQdrantPayload> qdrantPoint;

        if (string.IsNullOrEmpty(record.Id))
        {
            record.Id = Guid.NewGuid().ToString("N");
            qdrantPoint = QdrantPoint<DefaultQdrantPayload>.FromMemoryRecord(record);
            qdrantPoint.Id = Guid.NewGuid();

            this._log.LogTrace("Generate new Qdrant point ID {0} and record ID {1}", qdrantPoint.Id, record.Id);
        }
        else
        {
            qdrantPoint = QdrantPoint<DefaultQdrantPayload>.FromMemoryRecord(record);
            QdrantPoint<DefaultQdrantPayload>? existingPoint = await this._qdrantClient
                .GetVectorByPayloadIdAsync(index, record.Id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existingPoint == null)
            {
                qdrantPoint.Id = Guid.NewGuid();
                this._log.LogTrace("No record with ID {0} found, generated a new point ID {1}", record.Id, qdrantPoint.Id);
            }
            else
            {
                qdrantPoint.Id = existingPoint.Id;
                this._log.LogTrace("Point ID {0} found, updating...", qdrantPoint.Id);
            }
        }

        await this._qdrantClient.UpsertVectorsAsync(index, new[] { qdrantPoint }, cancellationToken).ConfigureAwait(false);

        return record.Id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        if (limit <= 0) { limit = int.MaxValue; }

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        var requiredTags = new List<IEnumerable<string>>();
        if (filters is { Count: > 0 })
        {
            requiredTags.AddRange(filters.Select(filter => filter.GetFilters().Select(x => $"{x.Key}{Constants.ReservedEqualsChar}{x.Value}")));
        }

        Embedding textEmbedding = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        List<(QdrantPoint<DefaultQdrantPayload>, double)> results;
        try
        {
            results = await this._qdrantClient.GetSimilarListAsync(
                collectionName: index,
                target: textEmbedding,
                scoreThreshold: minRelevance,
                requiredTags: requiredTags,
                limit: limit,
                withVectors: withEmbeddings,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (IndexNotFoundException e)
        {
            this._log.LogWarning(e, "Index not found");
            // Nothing to return
            yield break;
        }

        foreach (var point in results)
        {
            yield return (point.Item1.ToMemoryRecord(), point.Item2);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        if (limit <= 0) { limit = int.MaxValue; }

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        var requiredTags = new List<IEnumerable<string>>();
        if (filters is { Count: > 0 })
        {
            requiredTags.AddRange(filters.Select(filter => filter.GetFilters().Select(x => $"{x.Key}{Constants.ReservedEqualsChar}{x.Value}")));
        }

        List<QdrantPoint<DefaultQdrantPayload>> results;
        try
        {
            results = await this._qdrantClient.GetListAsync(
                collectionName: index,
                requiredTags: requiredTags,
                offset: 0,
                limit: limit,
                withVectors: withEmbeddings,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (IndexNotFoundException e)
        {
            this._log.LogWarning(e, "Index not found");
            // Nothing to return
            yield break;
        }

        foreach (var point in results)
        {
            yield return point.ToMemoryRecord();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        try
        {
            QdrantPoint<DefaultQdrantPayload>? existingPoint = await this._qdrantClient
                .GetVectorByPayloadIdAsync(index, record.Id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existingPoint == null)
            {
                this._log.LogTrace("No record with ID {0} found, nothing to delete", record.Id);
                return;
            }

            this._log.LogTrace("Point ID {0} found, deleting...", existingPoint.Id);
            await this._qdrantClient.DeleteVectorsAsync(index, new List<Guid> { existingPoint.Id }, cancellationToken).ConfigureAwait(false);
        }
        catch (IndexNotFoundException e)
        {
            this._log.LogInformation(e, "Index not found, nothing to delete");
        }
    }

    #region private ================================================================================

    // Note: "_" is allowed in Qdrant, but we normalize it to "-" for consistency with other DBs
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");
    private const string ValidSeparator = "-";

    private static string NormalizeIndexName(string index)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(index, nameof(index), "The index name is empty");
        index = s_replaceIndexNameCharsRegex.Replace(index.Trim().ToLowerInvariant(), ValidSeparator);

        return index.Trim();
    }

    #endregion
}
