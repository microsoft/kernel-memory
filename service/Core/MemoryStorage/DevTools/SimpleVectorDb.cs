// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.FileSystem.DevTools;

namespace Microsoft.KernelMemory.MemoryStorage.DevTools;

/// <summary>
/// Basic vector db implementation, designed for tests and demos only.
/// When searching, uses brute force comparing against all stored records.
/// </summary>
public class SimpleVectorDb : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SimpleVectorDb> _log;

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="config">Simple vector db settings</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public SimpleVectorDb(
        SimpleVectorDbConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<SimpleVectorDb>? log = null)
    {
        this._embeddingGenerator = embeddingGenerator;

        if (this._embeddingGenerator == null)
        {
            throw new SimpleVectorDbException("Embedding generator not configured");
        }

        this._log = log ?? DefaultLogger<SimpleVectorDb>.Instance;
        switch (config.StorageType)
        {
            case FileSystemTypes.Disk:
                this._fileSystem = new DiskFileSystem(config.Directory, this._log);
                break;

            case FileSystemTypes.Volatile:
                this._fileSystem = VolatileFileSystem.GetInstance(config.Directory, this._log);
                break;

            default:
                throw new ArgumentException($"Unknown storage type {config.StorageType}");
        }
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        return this._fileSystem.CreateVolumeAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        return this._fileSystem.ListVolumesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        return this._fileSystem.DeleteVolumeAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        await this._fileSystem.WriteFileAsync(index, "", EncodeId(record.Id), JsonSerializer.Serialize(record), cancellationToken).ConfigureAwait(false);
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
        if (limit <= 0) { limit = int.MaxValue; }

        index = NormalizeIndexName(index);

        var list = this.GetListAsync(index, filters, int.MaxValue, withEmbeddings, cancellationToken);
        var records = new Dictionary<string, MemoryRecord>();
        await foreach (MemoryRecord r in list.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            records[r.Id] = r;
        }

        // Calculate all the distances from the given vector
        // Note: this is a brute force search, very slow, not meant for production use cases
        var similarity = new Dictionary<string, double>();
        Embedding textEmbedding = await this._embeddingGenerator.GenerateEmbeddingAsync
            (text, cancellationToken).ConfigureAwait(false);
        foreach (var record in records)
        {
            similarity[record.Value.Id] = textEmbedding.CosineSimilarity(record.Value.Vector);
        }

        // Sort distances, from closest to most distant, and filter out irrelevant results
        IEnumerable<string> sorted =
            from entry in similarity
            where entry.Value >= minRelevance
            orderby entry.Value descending
            select entry.Key;

        // Return <count> vectors, including the calculated distance
        var count = 0;
        foreach (string id in sorted)
        {
            if (count++ < limit)
            {
                yield return (records[id], similarity[id]);
            }
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
        if (limit <= 0) { limit = int.MaxValue; }

        index = NormalizeIndexName(index);

        // Remove empty filters
        filters = filters?.Where(f => !f.IsEmpty()).ToList();

        IDictionary<string, string> list;
        try
        {
            list = await this._fileSystem.ReadAllFilesAsTextAsync(index, "", cancellationToken).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException)
        {
            // Index doesn't exist
            list = new Dictionary<string, string>();
        }

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
    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        index = NormalizeIndexName(index);
        return this._fileSystem.DeleteFileAsync(index, "", EncodeId(record.Id), cancellationToken);
    }

    #region private

    // Note: normalize "_" to "-" for consistency with other DBs
    private static readonly Regex s_replaceIndexNameCharsRegex = new(@"[\s|\\|/|.|_|:]");
    private const string ValidSeparator = "-";

    private static string NormalizeIndexName(string index)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            throw new ArgumentNullException(nameof(index), "The index name is empty");
        }

        index = s_replaceIndexNameCharsRegex.Replace(index.Trim().ToLowerInvariant(), ValidSeparator);

        return index.Trim();
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

    private static string EncodeId(string realId)
    {
        var bytes = Encoding.UTF8.GetBytes(realId);
        return Convert.ToBase64String(bytes).Replace('=', '_');
    }

    private static string DecodeId(string encodedId)
    {
        var bytes = Convert.FromBase64String(encodedId.Replace('_', '='));
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion
}
