﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage.Qdrant.Client;

namespace Microsoft.KernelMemory.MemoryStorage.Qdrant;

public class QdrantMemory : IVectorDb
{
    private readonly QdrantClient<DefaultQdrantPayload> _qdrantClient;
    private readonly ILogger<QdrantMemory> _log;

    public QdrantMemory(
        QdrantConfig config,
        ILogger<QdrantMemory>? log = null)
    {
        this._log = log ?? DefaultLogger<QdrantMemory>.Instance;
        this._qdrantClient = new QdrantClient<DefaultQdrantPayload>(endpoint: config.Endpoint, apiKey: config.APIKey);
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(
        string index, int vectorSize,
        CancellationToken cancellationToken = default)
    {
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
        index = this.NormalizeIndexName(index);
        if (string.Equals(index, Constants.DefaultIndex, StringComparison.OrdinalIgnoreCase))
        {
            this._log.LogWarning("The default index cannot be deleted");
            return Task.CompletedTask;
        }

        return this._qdrantClient.DeleteCollectionAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        index = this.NormalizeIndexName(index);

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
        Embedding embedding,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = this.NormalizeIndexName(index);
        if (limit <= 0) { limit = int.MaxValue; }

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        var requiredTags = new List<IEnumerable<string>>();
        if (filters is { Count: > 0 })
        {
            requiredTags.AddRange(filters.Select(filter => filter.GetFilters().Select(x => $"{x.Key}{Constants.ReservedEqualsChar}{x.Value}")));
        }

        List<(QdrantPoint<DefaultQdrantPayload>, double)> results = await this._qdrantClient.GetSimilarListAsync(
            collectionName: index,
            target: embedding,
            scoreThreshold: minRelevance,
            requiredTags: requiredTags,
            limit: limit,
            withVectors: withEmbeddings,
            cancellationToken: cancellationToken).ConfigureAwait(false);

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
        index = this.NormalizeIndexName(index);
        if (limit <= 0) { limit = int.MaxValue; }

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        var requiredTags = new List<IEnumerable<string>>();
        if (filters is { Count: > 0 })
        {
            requiredTags.AddRange(filters.Select(filter => filter.GetFilters().Select(x => $"{x.Key}{Constants.ReservedEqualsChar}{x.Value}")));
        }

        List<QdrantPoint<DefaultQdrantPayload>> results = await this._qdrantClient.GetListAsync(
            collectionName: index,
            requiredTags: requiredTags,
            offset: 0,
            limit: limit,
            withVectors: withEmbeddings,
            cancellationToken: cancellationToken).ConfigureAwait(false);

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
        index = this.NormalizeIndexName(index);

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

    private string NormalizeIndexName(string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            index = Constants.DefaultIndex;
        }

        return index.Trim();
    }
}
