// Copyright (c) Microsoft. All rights reserved.
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Http;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings.Providers;

/// <summary>
/// Ollama embedding generator implementation.
/// Communicates with a local Ollama instance via HTTP.
/// Default model: qwen3-embedding.
/// </summary>
public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<OllamaEmbeddingGenerator> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    /// <inheritdoc />
    public EmbeddingsTypes ProviderType => EmbeddingsTypes.Ollama;

    /// <inheritdoc />
    public string ModelName { get; }

    /// <inheritdoc />
    public int VectorDimensions { get; }

    /// <inheritdoc />
    public bool IsNormalized { get; }

    /// <summary>
    /// Creates a new Ollama embedding generator.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls.</param>
    /// <param name="baseUrl">Ollama base URL (e.g., http://localhost:11434).</param>
    /// <param name="model">Model name (e.g., qwen3-embedding).</param>
    /// <param name="vectorDimensions">Vector dimensions produced by the model.</param>
    /// <param name="isNormalized">Whether vectors are normalized.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="delayAsync">Optional delay function for retries (used for fast unit tests).</param>
    public OllamaEmbeddingGenerator(
        HttpClient httpClient,
        string baseUrl,
        string model,
        int vectorDimensions,
        bool isNormalized,
        ILogger<OllamaEmbeddingGenerator> logger,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this._httpClient = httpClient;
        this._baseUrl = baseUrl.TrimEnd('/');
        this.ModelName = model;
        this.VectorDimensions = vectorDimensions;
        this.IsNormalized = isNormalized;
        this._logger = logger;
        this._delayAsync = delayAsync ?? Task.Delay;

        this._logger.LogDebug("OllamaEmbeddingGenerator initialized: {BaseUrl}, model: {Model}, dimensions: {Dimensions}",
            this._baseUrl, this.ModelName, this.VectorDimensions);
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult> GenerateAsync(string text, CancellationToken ct = default)
    {
        var endpoint = $"{this._baseUrl}/api/embeddings";

        var request = new OllamaEmbeddingRequest
        {
            Model = this.ModelName,
            Prompt = text
        };

        this._logger.LogTrace("Calling Ollama embeddings API: {Endpoint}", endpoint);

        using var response = await HttpRetryPolicy.SendAsync(
            this._httpClient,
            requestFactory: () =>
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(request)
                };
                return httpRequest;
            },
            this._logger,
            ct,
            delayAsync: this._delayAsync).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(ct).ConfigureAwait(false);

        if (result?.Embedding == null || result.Embedding.Length == 0)
        {
            throw new InvalidOperationException("Ollama returned empty embedding");
        }

        this._logger.LogTrace("Ollama returned embedding with {Dimensions} dimensions", result.Embedding.Length);

        // Ollama API does not return token count
        return EmbeddingResult.FromVector(result.Embedding);
    }

    /// <inheritdoc />
    public async Task<EmbeddingResult[]> GenerateAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        // Ollama doesn't support batch embedding natively, so process one at a time
        var textList = texts.ToList();
        var results = new EmbeddingResult[textList.Count];

        this._logger.LogDebug("Generating {Count} embeddings via Ollama (sequential)", textList.Count);

        for (int i = 0; i < textList.Count; i++)
        {
            results[i] = await this.GenerateAsync(textList[i], ct).ConfigureAwait(false);
        }

        return results;
    }

    /// <summary>
    /// Request body for Ollama embeddings API.
    /// </summary>
    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from Ollama embeddings API.
    /// </summary>
    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
