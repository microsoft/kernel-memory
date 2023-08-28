// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
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
        this._storage = new TextFileStorage(config.Directory, this._log);
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
        Embedding<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        MemoryFilter? filter = null,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = this.GetListAsync(indexName, filter, limit, withEmbeddings, cancellationToken);
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
            var similarity = embedding.Vector.ToArray().CosineSimilarity(record.Value.Vector.Vector.ToArray());
            distances[record.Value.Id] = similarity;
        }

        // Sort distances, from closest to most distant
        IEnumerable<string> sorted = (from entry in distances orderby entry.Value descending select entry.Key);

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
        MemoryFilter? filter = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> list = await this._storage.ReadAllAsync(indexName, cancellationToken).ConfigureAwait(false);
        foreach (KeyValuePair<string, string> v in list)
        {
            var record = JsonSerializer.Deserialize<MemoryRecord>(v.Value);
            if (record == null) { continue; }

            var match = true;
            if (filter != null && !filter.IsEmpty())
            {
                foreach (KeyValuePair<string, List<string?>> tag in record.Tags)
                {
                    if (!match) { continue; }

                    for (int index = 0; match && index < tag.Value.Count; index++)
                    {
                        string? value = tag.Value[index];
                        match = match && (record.Tags.ContainsKey(tag.Key) && record.Tags[tag.Key].Contains(value));
                    }
                }
            }

            if (match)
            {
                yield return record;
            }
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string indexName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        return this._storage.DeleteAsync(indexName, record.Id, cancellationToken);
    }
}
