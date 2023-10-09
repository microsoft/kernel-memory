// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.MemoryStorage.DevTools;

public class SimpleVectorDb : ISemanticMemoryVectorDb
{
    private readonly ISimpleStorage _storage;
    private readonly ILogger<SimpleVectorDb> _log;

    public SimpleVectorDb(
        SimpleVectorDbConfig config,
        ILogger<SimpleVectorDb>? log = null)
    {
        this._log = log ?? DefaultLogger<SimpleVectorDb>.Instance;
        switch (config.StorageType)
        {
            case SimpleVectorDbConfig.StorageTypes.TextFile:
                this._storage = new TextFileStorage(config.Directory, this._log);
                break;

            case SimpleVectorDbConfig.StorageTypes.Volatile:
                this._storage = new VolatileStorage(this._log);
                break;

            default:
                throw new ArgumentException($"Unknown storage type {config.StorageType}");
        }
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string indexName, int vectorSize, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        // Note: delete only vectors! If the folder contains other files
        // the code will error out on purpose, to avoid data loss.
        var list = this.GetListAsync(indexName, null, int.MaxValue, true, cancellationToken);
        await foreach (MemoryRecord r in list.WithCancellation(cancellationToken))
        {
            await this.DeleteAsync(indexName, r, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string indexName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        await this._storage.WriteAsync(indexName, record.Id, JsonSerializer.Serialize(record), cancellationToken).ConfigureAwait(false);
        return record.Id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string indexName,
        Embedding embedding,
        int limit,
        double minRelevanceScore = 0,
        ICollection<MemoryFilter>? filters = null,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        var list = this.GetListAsync(indexName, filters, limit, withEmbeddings, cancellationToken);
        var records = new Dictionary<string, MemoryRecord>();
        await foreach (MemoryRecord r in list.WithCancellation(cancellationToken))
        {
            records[r.Id] = r;
        }

        // Calculate all the distances from the given vector
        // Note: this is a brute force search, very slow, not meant for production use cases
        var distances = new Dictionary<string, double>();
        foreach (var record in records)
        {
            distances[record.Value.Id] = embedding.CosineSimilarity(record.Value.Vector);
        }

        // Sort distances, from closest to most distant, and filter out irrelevant results
        IEnumerable<string> sorted =
            from entry in distances
            where entry.Value >= minRelevanceScore
            orderby entry.Value descending
            select entry.Key;

        // Return <count> vectors, including the calculated distance
        var count = 0;
        foreach (string id in sorted)
        {
            if (count++ < limit)
            {
                yield return (records[id], distances[id]);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string indexName,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (limit <= 0) { limit = int.MaxValue; }

        Dictionary<string, string> list = await this._storage.ReadAllAsync(indexName, cancellationToken).ConfigureAwait(false);
        foreach (KeyValuePair<string, string> v in list)
        {
            var record = JsonSerializer.Deserialize<MemoryRecord>(v.Value);
            if (record == null) { continue; }

            if (TagsMatchFilters(record.Tags, filters))
            {
                if (limit-- <= 0) { yield break; }

                yield return record;
            }
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string indexName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        return this._storage.DeleteAsync(indexName, record.Id, cancellationToken);
    }

    private static bool TagsMatchFilters(TagCollection tags, ICollection<MemoryFilter>? filters)
    {
        if (filters == null || filters.Count == 0) { return true; }

        // Verify that at least one filter matches (OR logic)
        foreach (MemoryFilter filter in filters)
        {
            var match = true;

            // Verify that all conditions are met (AND logic)
            foreach (KeyValuePair<string, List<string?>> condition in filter)
            {
                // Check if the tag name + value is present
                for (int index = 0; match && index < condition.Value.Count; index++)
                {
                    match = match && (tags.ContainsKey(condition.Key) && tags[condition.Key].Contains(condition.Value[index]));
                }
            }

            if (match) { return true; }
        }

        return false;
    }
}
