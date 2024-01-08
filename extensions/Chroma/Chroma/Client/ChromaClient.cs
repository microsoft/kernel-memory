// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema;
using Microsoft.KernelMemory.MemoryDb.Chroma.Client.Http.ApiSchema.RequestModels;

namespace Microsoft.KernelMemory.MemoryDb.Chroma.Client;

/// <summary>
/// An implementation of a client for the Chroma Vector DB. This class is used to
/// create, delete, and get embeddings data from Chroma Vector DB instance.
/// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable. Explanation - In this case, there is no need to dispose because either the NonDisposableHttpClientHandler or a custom HTTP client is being used.
internal sealed class ChromaClient
#pragma warning restore CA1001 // Types that own disposable fields should be disposable. Explanation - In this case, there is no need to dispose because either the NonDisposableHttpClientHandler or a custom HTTP client is being used.
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChromaClient"/> class.
    /// </summary>
    /// <param name="endpoint">Chroma server endpoint URL.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    /// <param name="httpClient">Optional HTTP client override.</param>
    public ChromaClient(
        string endpoint,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        this._endpoint = endpoint;
        this._httpClient = httpClient ?? new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        this._log = loggerFactory?.CreateLogger(typeof(ChromaClient)) ?? DefaultLogger<ChromaClient>.Instance;
    }

    /// <summary>
    /// Creates Chroma collection.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public async Task CreateCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Creating collection {0}", collectionName);
        using var request = CreateCollectionRequest.Create(collectionName).Build();
        await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns collection model instance by name.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Instance of <see cref="CollectionModel"/> model.</returns>
    public async Task<CollectionModel?> GetCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Getting collection {0}", collectionName);
        try
        {
            using var request = GetCollectionRequest.Create(collectionName).Build();
            (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<CollectionModel>(responseContent);
        }
        catch (ChromaCollectionNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Removes collection by name.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Deleting collection {0}", collectionName);
        try
        {
            using var request = DeleteCollectionRequest.Create(collectionName).Build();
            await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ChromaCollectionNotFoundException)
        {
        }
    }

    /// <summary>
    /// Returns all collection names.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous list of collection names.</returns>
    public async IAsyncEnumerable<string> ListCollectionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Listing collections");

        using var request = ListCollectionsRequest.Create().Build();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var collections = JsonSerializer.Deserialize<List<CollectionModel>>(responseContent);

        foreach (var collection in collections!)
        {
            yield return collection.Name;
        }
    }

    #region private ================================================================================

    private const string ApiRoute = "api/v1/";
    private static readonly Regex s_collectionNotFoundRegex = new(@"Collection [\w.\-():;'"" ]+ does not exist", RegexOptions.IgnoreCase);

    private readonly ILogger _log;
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint = null;

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        string endpoint = this._endpoint ?? this._httpClient.BaseAddress!.ToString();
        endpoint = this.SanitizeEndpoint(endpoint);
        string operationName = request.RequestUri!.ToString();
        request.RequestUri = new Uri(new Uri(endpoint), operationName);

        HttpResponseMessage response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            this._log.LogWarning("Chroma responded with 404 Not Found: {0}", responseContent);
            throw new ChromaCollectionNotFoundException();
        }

        if (response.StatusCode == HttpStatusCode.InternalServerError && s_collectionNotFoundRegex.IsMatch(responseContent))
        {
            this._log.LogWarning("Chroma responded with 500 Internal server error: {0}", responseContent);
            throw new ChromaCollectionNotFoundException();
        }

        if (response.IsSuccessStatusCode)
        {
            this._log.LogTrace("Chroma responded successfully");
        }
        else
        {
            this._log.LogWarning("Chroma responded with error: {0}, {1}", response.StatusCode, responseContent);
            throw new ChromaException($"Chroma HTTP error {response.StatusCode}");
        }

        return (response, responseContent);
    }

    private string SanitizeEndpoint(string endpoint)
    {
        StringBuilder builder = new(endpoint);

        if (!endpoint.EndsWith('/')) { builder.Append('/'); }

        builder.Append(ApiRoute);

        return builder.ToString();
    }

    #endregion
}
