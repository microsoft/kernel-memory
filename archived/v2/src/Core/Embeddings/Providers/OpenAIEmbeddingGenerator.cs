// Copyright (c) Microsoft. All rights reserved.
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Http;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings.Providers;

/// <summary>
/// OpenAI embedding generator implementation.
/// Communicates with the OpenAI API (or compatible endpoints).
/// Supports batch embedding requests.
/// </summary>
public sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly int _batchSize;
    private readonly ILogger<OpenAIEmbeddingGenerator> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    /// <inheritdoc />
    public EmbeddingsTypes ProviderType => EmbeddingsTypes.OpenAI;

    /// <inheritdoc />
    public string ModelName { get; }

    /// <inheritdoc />
    public int VectorDimensions { get; }

    /// <inheritdoc />
    public bool IsNormalized { get; }

    /// <summary>
    /// Creates a new OpenAI embedding generator.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls.</param>
    /// <param name="apiKey">OpenAI API key.</param>
    /// <param name="model">Model name (e.g., text-embedding-ada-002).</param>
    /// <param name="vectorDimensions">Vector dimensions produced by the model.</param>
    /// <param name="isNormalized">Whether vectors are normalized.</param>
    /// <param name="baseUrl">Optional custom base URL for OpenAI-compatible APIs.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="batchSize">Maximum number of texts per API request.</param>
    /// <param name="delayAsync">Optional delay function for retries (used for fast unit tests).</param>
    public OpenAIEmbeddingGenerator(
        HttpClient httpClient,
        string apiKey,
        string model,
        int vectorDimensions,
        bool isNormalized,
        string? baseUrl,
        ILogger<OpenAIEmbeddingGenerator> logger,
        int batchSize,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey));
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1, nameof(batchSize));

        this._httpClient = httpClient;
        this._apiKey = apiKey;
        this._baseUrl = (baseUrl ?? Constants.EmbeddingDefaults.DefaultOpenAIBaseUrl).TrimEnd('/');
        this._batchSize = batchSize;
        this.ModelName = model;
        this.VectorDimensions = vectorDimensions;
        this.IsNormalized = isNormalized;
        this._logger = logger;
        this._delayAsync = delayAsync ?? Task.Delay;

        this._logger.LogDebug("OpenAIEmbeddingGenerator initialized: {BaseUrl}, model: {Model}, dimensions: {Dimensions}",
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

        var allResults = new List<EmbeddingResult>(textArray.Length);
        foreach (var chunk in Chunk(textArray, this._batchSize))
        {
            var chunkResults = await this.GenerateBatchAsync(chunk, ct).ConfigureAwait(false);
            allResults.AddRange(chunkResults);
        }

        return allResults.ToArray();
    }

    private async Task<EmbeddingResult[]> GenerateBatchAsync(string[] textArray, CancellationToken ct)
    {
        var endpoint = $"{this._baseUrl}/v1/embeddings";

        var request = new OpenAIEmbeddingRequest
        {
            Model = this.ModelName,
            Input = textArray
        };

        this._logger.LogTrace("Calling OpenAI embeddings API: {Endpoint}, batch size: {BatchSize}",
            endpoint, textArray.Length);

        using var response = await HttpRetryPolicy.SendAsync(
            this._httpClient,
            requestFactory: () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this._apiKey);
                httpRequest.Content = JsonContent.Create(request);
                return httpRequest;
            },
            this._logger,
            ct,
            delayAsync: this._delayAsync).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(ct).ConfigureAwait(false);

        if (result?.Data == null || result.Data.Length == 0)
        {
            throw new InvalidOperationException("OpenAI returned empty embedding response");
        }

        // Sort by index to ensure correct ordering
        var sortedData = result.Data.OrderBy(d => d.Index).ToArray();

        // Get total token count from API response
        var totalTokens = result.Usage?.TotalTokens;

        this._logger.LogTrace("OpenAI returned {Count} embeddings, usage: {TotalTokens} tokens",
            sortedData.Length, totalTokens);

        // Calculate per-embedding token count if total tokens available
        // For batch requests, we distribute tokens evenly across embeddings (approximation)
        int? perEmbeddingTokens = null;
        if (totalTokens.HasValue && sortedData.Length > 0)
        {
            perEmbeddingTokens = totalTokens.Value / sortedData.Length;
        }

        // Create EmbeddingResult for each embedding with token count
        var results = new EmbeddingResult[sortedData.Length];
        for (int i = 0; i < sortedData.Length; i++)
        {
            results[i] = perEmbeddingTokens.HasValue
                ? EmbeddingResult.FromVectorWithTokens(sortedData[i].Embedding, perEmbeddingTokens.Value)
                : EmbeddingResult.FromVector(sortedData[i].Embedding);
        }

        return results;
    }

    private static IEnumerable<string[]> Chunk(string[] items, int chunkSize)
    {
        for (int i = 0; i < items.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, items.Length - i);
            var chunk = new string[length];
            Array.Copy(items, i, chunk, 0, length);
            yield return chunk;
        }
    }

    /// <summary>
    /// Request body for OpenAI embeddings API.
    /// </summary>
    private sealed class OpenAIEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Response from OpenAI embeddings API.
    /// </summary>
    private sealed class OpenAIEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    private sealed class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
