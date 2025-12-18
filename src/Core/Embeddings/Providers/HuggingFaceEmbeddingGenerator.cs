// Copyright (c) Microsoft. All rights reserved.
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings.Providers;

/// <summary>
/// HuggingFace Inference API embedding generator implementation.
/// Communicates with the HuggingFace serverless Inference API.
/// Default model: sentence-transformers/all-MiniLM-L6-v2.
/// </summary>
public sealed class HuggingFaceEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<HuggingFaceEmbeddingGenerator> _logger;

    /// <inheritdoc />
    public EmbeddingsTypes ProviderType => EmbeddingsTypes.HuggingFace;

    /// <inheritdoc />
    public string ModelName { get; }

    /// <inheritdoc />
    public int VectorDimensions { get; }

    /// <inheritdoc />
    public bool IsNormalized { get; }

    /// <summary>
    /// Creates a new HuggingFace embedding generator.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls.</param>
    /// <param name="apiKey">HuggingFace API token (HF_TOKEN).</param>
    /// <param name="model">Model name (e.g., sentence-transformers/all-MiniLM-L6-v2).</param>
    /// <param name="vectorDimensions">Vector dimensions produced by the model.</param>
    /// <param name="isNormalized">Whether vectors are normalized.</param>
    /// <param name="baseUrl">Optional custom base URL for inference endpoints.</param>
    /// <param name="logger">Logger instance.</param>
    public HuggingFaceEmbeddingGenerator(
        HttpClient httpClient,
        string apiKey,
        string model,
        int vectorDimensions,
        bool isNormalized,
        string? baseUrl,
        ILogger<HuggingFaceEmbeddingGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this._httpClient = httpClient;
        this._apiKey = apiKey;
        this._baseUrl = (baseUrl ?? Constants.EmbeddingDefaults.DefaultHuggingFaceBaseUrl).TrimEnd('/');
        this.ModelName = model;
        this.VectorDimensions = vectorDimensions;
        this.IsNormalized = isNormalized;
        this._logger = logger;

        this._logger.LogDebug("HuggingFaceEmbeddingGenerator initialized: {BaseUrl}, model: {Model}, dimensions: {Dimensions}",
            this._baseUrl, this.ModelName, this.VectorDimensions);
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult> GenerateAsync(string text, CancellationToken ct = default)
    {
        var results = await this.GenerateAsync(new[] { text }, ct).ConfigureAwait(false);
        return results[0];
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult[]> GenerateAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textArray = texts.ToArray();
        if (textArray.Length == 0)
        {
            return [];
        }

        var endpoint = $"{this._baseUrl}/models/{this.ModelName}";

        var request = new HuggingFaceRequest
        {
            Inputs = textArray
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this._apiKey);
        httpRequest.Content = JsonContent.Create(request);

        this._logger.LogTrace("Calling HuggingFace embeddings API: {Endpoint}, batch size: {BatchSize}",
            endpoint, textArray.Length);

        var response = await this._httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // HuggingFace returns array of embeddings directly: float[][]
        var embeddings = await response.Content.ReadFromJsonAsync<float[][]>(ct).ConfigureAwait(false);

        if (embeddings == null || embeddings.Length == 0)
        {
            throw new InvalidOperationException("HuggingFace returned empty embedding response");
        }

        this._logger.LogTrace("HuggingFace returned {Count} embeddings with {Dimensions} dimensions each",
            embeddings.Length, embeddings[0].Length);

        // HuggingFace API does not return token count
        var results = new EmbeddingResult[embeddings.Length];
        for (int i = 0; i < embeddings.Length; i++)
        {
            results[i] = EmbeddingResult.FromVector(embeddings[i]);
        }

        return results;
    }

    /// <summary>
    /// Request body for HuggingFace Inference API.
    /// </summary>
    private sealed class HuggingFaceRequest
    {
        [JsonPropertyName("inputs")]
        public string[] Inputs { get; set; } = Array.Empty<string>();
    }
}
