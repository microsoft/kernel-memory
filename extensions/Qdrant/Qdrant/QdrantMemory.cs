// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryDb.Qdrant.Client;
using Microsoft.KernelMemory.MemoryStorage;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Microsoft.KernelMemory.MemoryDb.Qdrant;

/// <summary>
/// Qdrant connector for Kernel Memory
/// TODO:
/// * allow using more Qdrant specific filtering logic
/// </summary>
[Experimental("KMEXP03")]
public sealed class QdrantMemory : IMemoryDb, IMemoryDbUpsertBatch
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantMemory> _log;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Qdrant connector configuration</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public QdrantMemory(
        QdrantConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILoggerFactory? loggerFactory = null)
    {
        this._embeddingGenerator = embeddingGenerator;

        if (this._embeddingGenerator == null)
        {
            throw new QdrantException("Embedding generator not configured");
        }

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<QdrantMemory>();
        this._qdrantClient = new QdrantClient(new Uri(config.Endpoint), apiKey: config.APIKey, loggerFactory: loggerFactory);
    }

    /// <inheritdoc />
    public async Task CreateIndexAsync(
        string index, int vectorSize,
        CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        try
        {
            await this._qdrantClient.CreateCollectionAsync(index, new VectorParams
            {
                Distance = Distance.Cosine,
                Size = (ulong)vectorSize
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
        {
            this._log.LogInformation("Index already exists");
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        return await this._qdrantClient
            .ListCollectionsAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        try
        {
            index = NormalizeIndexName(index);
            await this._qdrantClient.DeleteCollectionAsync(index, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (QdrantException)
        {
            this._log.LogInformation("Index not found, nothing to delete");
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(
        string index,
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        var result = this.UpsertBatchAsync(index, [record], cancellationToken);
        var id = await result.SingleAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> UpsertBatchAsync(string index, IEnumerable<MemoryRecord> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);

        // Call ToList to avoid multiple enumerations (CA1851: Possible multiple enumerations of 'IEnumerable' collection. Consider using an implementation that avoids multiple enumerations).
        var localRecords = records.ToList();

        var qdrantPoints = new List<PointStruct>();
        foreach (var record in localRecords)
        {
            PointStruct qdrantPoint;

            if (string.IsNullOrEmpty(record.Id))
            {
                record.Id = Guid.NewGuid().ToString("N");
                qdrantPoint = QdrantPointStruct.FromMemoryRecord(record);
                qdrantPoint.Id = Guid.NewGuid();

                this._log.LogTrace("Generate new Qdrant point ID {0} and record ID {1}", qdrantPoint.Id, record.Id);
            }
            else
            {
                qdrantPoint = QdrantPointStruct.FromMemoryRecord(record);
                IReadOnlyList<RetrievedPoint> retrievedPoints = await this._qdrantClient
                    .RetrieveAsync(index, new Guid(record.Id), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (retrievedPoints.Count == 0)
                {
                    qdrantPoint.Id = Guid.NewGuid();
                    this._log.LogTrace("No record with ID {0} found, generated a new point ID {1}", record.Id, qdrantPoint.Id);
                }
                else
                {
                    qdrantPoint.Id = retrievedPoints[0].Id;
                    this._log.LogTrace("Point ID {0} found, updating...", qdrantPoint.Id);
                }
            }

            qdrantPoints.Add(qdrantPoint);
        }

        await this._qdrantClient.UpsertAsync(index, qdrantPoints, cancellationToken: cancellationToken).ConfigureAwait(false);

        foreach (var record in localRecords)
        {
            yield return record.Id;
        }
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

        IReadOnlyList<ScoredPoint> results;
        try
        {
            results = await this._qdrantClient.SearchAsync(
                collectionName: index,
                vector: textEmbedding.Data,
                scoreThreshold: Convert.ToSingle(minRelevance),
                filter: QdrantFilter.BuildFilter(requiredTags),
                limit: Convert.ToUInt64(limit),
                vectorsSelector: withEmbeddings,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.NotFound)
        {
            this._log.LogWarning(e, "Index not found");
            // Nothing to return
            yield break;
        }

        foreach (var point in results)
        {
            yield return (QdrantPointStruct.ToMemoryRecord(point, withEmbeddings), point.Score);
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

        ScrollResponse results;
        try
        {
            results = await this._qdrantClient.ScrollAsync(
                collectionName: index,
                filter: QdrantFilter.BuildFilter(requiredTags),
                offset: 0,
                limit: Convert.ToUInt32(limit),
                vectorsSelector: withEmbeddings,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.NotFound)
        {
            this._log.LogWarning(e, "Index not found");
            // Nothing to return
            yield break;
        }

        foreach (var point in results.Result)
        {
            yield return QdrantPointStruct.ToMemoryRecord(point, withEmbeddings);
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
            IReadOnlyList<RetrievedPoint> existingPoints = await this._qdrantClient
                .RetrieveAsync(index, new Guid(record.Id), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existingPoints.Count == 0)
            {
                this._log.LogTrace("No record with ID {0} found, nothing to delete", record.Id);
                return;
            }

            RetrievedPoint existingPoint = existingPoints[0];
            this._log.LogTrace("Point ID {0} found, deleting...", existingPoint.Id);
            await this._qdrantClient.DeleteAsync(index, new Guid(existingPoint.Id.Uuid), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.NotFound)
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
