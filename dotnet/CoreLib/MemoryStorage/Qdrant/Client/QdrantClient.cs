﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.MemoryStorage.Qdrant.Client.Http;

namespace Microsoft.SemanticMemory.MemoryStorage.Qdrant.Client;

/// <summary>
/// An implementation of a client for the Qdrant Vector Database. This class is used to
/// connect, create, delete, and get embeddings data from a Qdrant Vector Database instance.
/// </summary>
internal sealed class QdrantClient<T> where T : DefaultQdrantPayload, new()
{
    private readonly string? _apiKey;

    /// <summary>
    /// Represents a singleton implementation of <see cref="HttpClientHandler"/> that is not disposable.
    /// </summary>
    private sealed class NonDisposableHttpClientHandler : HttpClientHandler
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="NonDisposableHttpClientHandler"/>.
        /// </summary>
        internal static NonDisposableHttpClientHandler Instance { get; } = new();

        /// <summary>
        /// Private constructor to prevent direct instantiation of the class.
        /// </summary>
        private NonDisposableHttpClientHandler()
        {
            this.CheckCertificateRevocationList = true;
        }

#pragma warning disable CA2215 // nothing to dispose
        /// <summary>
        /// Disposes the underlying resources.
        /// This implementation does nothing to prevent unintended disposal, as it may affect all references.
        /// </summary>
        /// <param name="disposing">True if called from <see cref="Dispose"/>, false if called from a finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            // Do nothing if called explicitly from Dispose, as it may unintentionally affect all references.
            // The base.Dispose(disposing) is not called to avoid invoking the disposal of HttpClientHandler resources.
            // This implementation assumes that the HttpClientHandler is being used as a singleton and should not be disposed directly.
        }
#pragma warning restore CA2215
    }

    /// <summary>
    /// Initializes a new instance of this class.
    /// </summary>
    /// <param name="endpoint">The Qdrant Vector Database endpoint.</param>
    /// <param name="apiKey">API key for Qdrant cloud</param>
    /// <param name="httpClient">Optional HTTP client override.</param>
    /// <param name="log">Application logger.</param>
    public QdrantClient(
        string endpoint,
        string? apiKey = null,
        HttpClient? httpClient = null,
        ILogger<QdrantClient<T>>? log = null)
    {
        this._log = log ?? DefaultLogger<QdrantClient<T>>.Instance;
        this._apiKey = apiKey;
        this._httpClient = httpClient ?? new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        this._httpClient.BaseAddress = SanitizeEndpoint(endpoint);
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="vectorSize">Size of the vectors stored</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task CreateCollectionAsync(
        string collectionName,
        int vectorSize,
        CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Creating collection {0}", collectionName);

        using HttpRequestMessage request = CreateCollectionRequest
            .Create(collectionName, vectorSize, QdrantDistanceType.Cosine)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        // Creation is idempotent, ignore error (and for now ignore vector size)
        if (response.StatusCode == HttpStatusCode.BadRequest && content.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            this._log.LogDebug("Collection {0} already exists", collectionName);
            return;
        }

        this.ValidateResponse(response, content, nameof(this.CreateCollectionAsync));
    }

    /// <summary>
    /// Delete a collection.
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task DeleteCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Deleting collection {0}", collectionName);

        using HttpRequestMessage request = DeleteCollectionRequest.Create(collectionName).Build();
        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        // Deletion is idempotent, ignore error
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        this.ValidateResponse(response, content, nameof(this.DeleteCollectionAsync));
    }

    /// <summary>
    /// Write a set of vectors. Existing vectors ar updated.
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="vectors">List of vectors to write</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task UpsertVectorsAsync(
        string collectionName,
        IEnumerable<QdrantPoint<T>> vectors,
        CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Upserting vectors into {0}", collectionName);

        using var request = UpsertVectorRequest<T>.Create(collectionName)
            .UpsertRange(vectors)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        this.ValidateResponse(response, content, nameof(this.UpsertVectorsAsync));

        if (JsonSerializer.Deserialize<UpsertVectorResponse>(content)?.Status != "ok")
        {
            this._log.LogWarning("Vector upserts failed");
        }
    }

    /// <summary>
    /// Fetch a vector by payload ID (string).
    /// Qdrant vector ID (int/guid) is not used.
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="payloadId">Unique ID stored in the payload</param>
    /// <param name="withVector">Whether to include vectors</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Vector matching the given ID</returns>
    public async Task<QdrantPoint<T>?> GetVectorByPayloadIdAsync(
        string collectionName,
        string payloadId,
        bool withVector = false,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = ScrollVectorsRequest.Create(collectionName)
            .HavingExternalId(payloadId)
            .IncludePayLoad()
            .TakeFirst()
            .IncludeVectorData(withVector)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        this.ValidateResponse(response, content, nameof(this.GetVectorByPayloadIdAsync));

        var data = JsonSerializer.Deserialize<ScrollVectorsResponse<T>>(content);
        if (data == null)
        {
            this._log.LogError("Unable to deserialize Search response");
            throw new QdrantException("Unable to deserialize Search response");
        }

        if (!data.Results.Points.Any())
        {
            this._log.LogDebug("Vector not found");
            return null;
        }

        QdrantPoint<T> vector = data.Results.Points.First();
        this._log.LogDebug("Vector found: {0}", vector.Id);

        return new QdrantPoint<T>
        {
            Id = vector.Id,
            Vector = vector.Vector,
            Payload = vector.Payload
        };
    }

    /// <summary>
    /// Delete a list of vectors
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="vectorIds">List of vector IDs</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    public async Task DeleteVectorsAsync(string collectionName, IList<Guid> vectorIds, CancellationToken cancellationToken)
    {
        this._log.LogTrace("Deleting vectors");
        using var request = DeleteVectorsRequest.DeleteFrom(collectionName)
            .DeleteRange(vectorIds)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        this.ValidateResponse(response, content, nameof(this.DeleteVectorsAsync));
    }

    /// <summary>
    /// Fetch a list of vectors
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="requiredTags">Optional filtering rules</param>
    /// <param name="offset">Pagination offset</param>
    /// <param name="limit">Max number of vectors to return</param>
    /// <param name="withVectors">Whether to include vectors</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of vectors</returns>
    public async Task<List<QdrantPoint<T>>> GetListAsync(
        string collectionName,
        IEnumerable<string>? requiredTags = null,
        int offset = 0,
        int limit = 1,
        bool withVectors = false,
        CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Fetch list of {0} vectors starting from {1}", limit, offset);

        using HttpRequestMessage request = ScrollVectorsRequest
            .Create(collectionName)
            .HavingTags(requiredTags)
            .IncludePayLoad()
            .IncludeVectorData(withVectors)
            .FromPosition(offset)
            .Take(limit)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        this.ValidateResponse(response, content, nameof(this.GetListAsync));

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            this._log.LogDebug("No vectors found");
            return new List<QdrantPoint<T>>();
        }

        var data = JsonSerializer.Deserialize<ScrollVectorsResponse<T>>(content);
        if (data == null)
        {
            this._log.LogError("Unable to deserialize Scroll response");
            throw new QdrantException("Unable to deserialize Scroll response");
        }

        if (!data.Results.Points.Any())
        {
            this._log.LogDebug("No vectors found");
            return new List<QdrantPoint<T>>();
        }

        return data.Results.Points.ToList();
    }

    /// <summary>
    /// Find similar vectors
    /// TODO: return IAsyncEnumerable
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="target">Vector to compare to</param>
    /// <param name="minSimilarityScore">Minimum similarity required to be included in the results</param>
    /// <param name="limit">Max number of vectors to return</param>
    /// <param name="withVectors">Whether to include vectors</param>
    /// <param name="requiredTags">Optional filtering rules</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of vectors</returns>
    public async Task<List<(QdrantPoint<T>, double)>> GetSimilarListAsync(
        string collectionName,
        Embedding target,
        double minSimilarityScore,
        int limit = 1,
        bool withVectors = false,
        IEnumerable<string>? requiredTags = null,
        CancellationToken cancellationToken = default)
    {
        this._log.LogTrace("Searching top {0} nearest vectors", limit);

        Verify.NotNull(target, "The given vector is NULL");

        using HttpRequestMessage request = SearchVectorsRequest
            .Create(collectionName)
            .SimilarTo(target)
            .HavingTags(requiredTags)
            .WithScoreThreshold(minSimilarityScore)
            .IncludePayLoad()
            .IncludeVectorData(withVectors)
            .Take(limit)
            .Build();

        var (response, content) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        this.ValidateResponse(response, content, nameof(this.GetSimilarListAsync));

        var result = new List<(QdrantPoint<T>, double)>();

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            this._log.LogDebug("No vectors found");
            return result;
        }

        var data = JsonSerializer.Deserialize<SearchVectorsResponse<T>>(content);
        if (data == null)
        {
            this._log.LogError("Unable to deserialize Search response");
            throw new QdrantException("Unable to deserialize Search response");
        }

        if (!data.Results.Any())
        {
            this._log.LogDebug("No vectors found");
            return result;
        }

        foreach (SearchVectorsResponse<T>.ScoredPoint vector in data.Results)
        {
            result.Add((new QdrantPoint<T>
            {
                Id = vector.Id,
                Vector = vector.Vector,
                Payload = vector.Payload
            }, vector.Score ?? 0.0));
        }

        return result;
    }

    private void ValidateResponse(HttpResponseMessage response, string content, string methodName)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "{0} failed: {0}, {1}", methodName, e.Message, content);
            throw;
        }
    }

    #region private ================================================================================

    private readonly ILogger<QdrantClient<T>> _log;
    private readonly HttpClient _httpClient;

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(this._apiKey))
        {
            request.Headers.Add("api-key", this._apiKey);
        }

        HttpResponseMessage response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            this._log.LogTrace("Qdrant responded successfully");
        }
        else
        {
            if (!responseContent.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                this._log.LogWarning("Qdrant responded with error: {0}, {1}", response.StatusCode, responseContent);
            }
        }

        return (response, responseContent);
    }

    private static Uri SanitizeEndpoint(string endpoint, int? port = null)
    {
        ValidateUrl(nameof(endpoint), endpoint);

        UriBuilder builder = new(endpoint);
        if (port.HasValue) { builder.Port = port.Value; }

        return builder.Uri;
    }

    private static void ValidateUrl(string name, string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException($"The {name} is empty", name);
        }

        bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);
        if (!result || string.IsNullOrEmpty(uri?.Host))
        {
            throw new ArgumentException($"The {name} `{url}` is not valid", name);
        }
    }

    #endregion
}
