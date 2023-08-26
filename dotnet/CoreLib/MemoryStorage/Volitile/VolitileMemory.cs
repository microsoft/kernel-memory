// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using Microsoft.SemanticKernel.Memory.Collections;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.MemoryStorage.Volitile;

public class VolitileMemory : ISemanticMemoryVectorDb
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MemoryRecord>> _store = new();

    private readonly ILogger<VolitileMemory> _log;

    public VolitileMemory(ILogger<VolitileMemory>? log = null)
    {
        this._log = log ?? DefaultLogger<VolitileMemory>.Instance;
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string indexName, int vectorSize, CancellationToken cancellationToken = default)
    {
        _store.TryAdd(indexName, new ConcurrentDictionary<string, MemoryRecord>());

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        if (!_store.TryRemove(indexName, out _))
        {
            return Task.FromException(new InvalidOperationException($"Could not delete collection {indexName}"));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> UpsertAsync(string indexName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        if (this.TryGetCollection(indexName, out var collectionDict, create: false))
        {
            collectionDict[record.Id] = record;
        }
        else
        {
            return Task.FromException<string>(new InvalidOperationException($"Attempted to access a memory collection that does not exist: {indexName}"));
        }

        return Task.FromResult(record.Id);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string indexName,
        Embedding<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        MemoryFilter? filter = null,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return AsyncEnumerable.Empty<(MemoryRecord, double)>();
        }

        ICollection<MemoryRecord>? embeddingCollection = null;
        if (this.TryGetCollection(indexName, out var collectionDict))
        {
            embeddingCollection = collectionDict.Values;
        }

        if (embeddingCollection == null || embeddingCollection.Count == 0)
        {
            return AsyncEnumerable.Empty<(MemoryRecord, double)>();
        }

        TopNCollection<MemoryRecord> embeddings = new(limit);

        var embeddingSpan = embedding.AsReadOnlySpan();
        foreach (var record in FilterEmbeddings(filter, embeddingCollection))
        {
            double similarity = embeddingSpan.CosineSimilarity(record.Vector.AsReadOnlySpan());
            if (similarity >= minRelevanceScore)
            {
                embeddings.Add(new(record, similarity));
            }
        }

        embeddings.SortByScore();

        return embeddings.Select(x => (x.Value, x.Score.Value)).ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<MemoryRecord> GetListAsync(
        string indexName,
        MemoryFilter? filter = null,
        int limit = 1,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return AsyncEnumerable.Empty<MemoryRecord>();
        }

        ICollection<MemoryRecord>? embeddingCollection = null;
        if (this.TryGetCollection(indexName, out var collectionDict))
        {
            embeddingCollection = collectionDict.Values;
        }

        if (embeddingCollection == null || embeddingCollection.Count == 0)
        {
            return AsyncEnumerable.Empty<MemoryRecord>();
        }

        return FilterEmbeddings(filter, embeddingCollection).Take(limit).ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string indexName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var id = record.Id;

        this._log.LogDebug("Deleting record {0} from index {1}", id, indexName);

        if (this.TryGetCollection(indexName, out var collectionDict))
        {
            collectionDict.TryRemove(id, out var _);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<MemoryRecord> FilterEmbeddings(
        MemoryFilter? filter,
        IEnumerable<MemoryRecord> embeddingCollection)
    {
        if (filter == null)
        {
            return embeddingCollection;
        }

        return embeddingCollection.Where(
            e =>
            {
                if (filter.Keys.Count == 0)
                {
                    return true;
                }

                var commonKeys = e.Tags.Keys.Intersect(filter.Keys).ToArray();
                if (commonKeys.Length != filter.Count)
                {
                    return false;
                }

                foreach (var key in commonKeys)
                {
                    var filterSet = filter[key].ToHashSet();
                    if (filterSet.Intersect(e.Tags[key]).Count() != filterSet.Count)
                    {
                        return false;
                    }
                }

                return true;
            });
    }

    private bool TryGetCollection(
        string name,
        [NotNullWhen(true)] out ConcurrentDictionary<string,
        MemoryRecord>? collection,
        bool create = false)
    {
        if (_store.TryGetValue(name, out collection))
        {
            return true;
        }

        if (create)
        {
            collection = new ConcurrentDictionary<string, MemoryRecord>();
            return _store.TryAdd(name, collection);
        }

        collection = null;
        return false;
    }
}
