// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryDb.Chroma.Client;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.MemoryDb.Chroma;

/// <summary>
/// KM connector for Chroma https://www.trychroma.com
/// </summary>
public class ChromaMemory : IMemoryDb
{
    private readonly ChromaClient _client;
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<ChromaMemory> _log;

    /// <summary>
    /// Create new instance of Chroma Memory DB
    /// </summary>
    /// <param name="config">Chroma settings</param>
    /// <param name="embeddingGenerator">Text embedding generator</param>
    /// <param name="log">Application logger</param>
    public ChromaMemory(
        ChromaConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<ChromaMemory>? log = null)
    {
        config.Validate();

        this._embeddingGenerator = embeddingGenerator;

        if (this._embeddingGenerator == null)
        {
            throw new ChromaException("Embedding generator not configured");
        }

        this._log = log ?? DefaultLogger<ChromaMemory>.Instance;

        this._client = new ChromaClient(config.Endpoint);
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        return this._client.CreateCollectionAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        return await this._client.ListCollectionsAsync(cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        await this._client.DeleteCollectionAsync(index, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(string index, string text, ICollection<MemoryFilter>? filters = null, double minRelevance = 0, int limit = 1, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<MemoryRecord> GetListAsync(string index, ICollection<MemoryFilter>? filters = null, int limit = 1, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
