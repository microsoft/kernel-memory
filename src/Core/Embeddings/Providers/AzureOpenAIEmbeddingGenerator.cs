// Copyright (c) Microsoft. All rights reserved.
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KernelMemory.Core.Config.Enums;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Embeddings.Providers;

/// <summary>
/// Azure OpenAI embedding generator implementation.
/// Communicates with Azure OpenAI Service.
/// Supports API key authentication (managed identity would require Azure.Identity package).
/// </summary>
public sealed class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _deployment;
    private readonly string _apiKey;
    private readonly ILogger<AzureOpenAIEmbeddingGenerator> _logger;

    /// <inheritdoc />
    public EmbeddingsTypes ProviderType => EmbeddingsTypes.AzureOpenAI;

    /// <inheritdoc />
    public string ModelName { get; }

    /// <inheritdoc />
    public int VectorDimensions { get; }

    /// <inheritdoc />
    public bool IsNormalized { get; }

    /// <summary>
    /// Creates a new Azure OpenAI embedding generator with API key authentication.
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls.</param>
    /// <param name="endpoint">Azure OpenAI endpoint (e.g., https://myservice.openai.azure.com).</param>
    /// <param name="deployment">Deployment name in Azure.</param>
    /// <param name="model">Model name for identification.</param>
    /// <param name="apiKey">Azure OpenAI API key.</param>
    /// <param name="vectorDimensions">Vector dimensions produced by the model.</param>
    /// <param name="isNormalized">Whether vectors are normalized.</param>
    /// <param name="logger">Logger instance.</param>
    public AzureOpenAIEmbeddingGenerator(
        HttpClient httpClient,
        string endpoint,
        string deployment,
        string model,
        string apiKey,
        int vectorDimensions,
        bool isNormalized,
        ILogger<AzureOpenAIEmbeddingGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));
        ArgumentNullException.ThrowIfNull(deployment, nameof(deployment));
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        ArgumentNullException.ThrowIfNull(apiKey, nameof(apiKey));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this._httpClient = httpClient;
        this._endpoint = endpoint.TrimEnd('/');
        this._deployment = deployment;
        this._apiKey = apiKey;
        this.ModelName = model;
        this.VectorDimensions = vectorDimensions;
        this.IsNormalized = isNormalized;
        this._logger = logger;

        this._logger.LogDebug("AzureOpenAIEmbeddingGenerator initialized: {Endpoint}, deployment: {Deployment}, model: {Model}",
            this._endpoint, this._deployment, this.ModelName);
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

        var url = $"{this._endpoint}/openai/deployments/{this._deployment}/embeddings?api-version={Constants.EmbeddingDefaults.AzureOpenAIApiVersion}";

        var request = new AzureEmbeddingRequest
        {
            Input = textArray
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("api-key", this._apiKey);
        httpRequest.Content = JsonContent.Create(request);

        this._logger.LogTrace("Calling Azure OpenAI embeddings API: deployment={Deployment}, batch size: {BatchSize}",
            this._deployment, textArray.Length);

        var response = await this._httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureEmbeddingResponse>(ct).ConfigureAwait(false);

        if (result?.Data == null || result.Data.Length == 0)
        {
            throw new InvalidOperationException("Azure OpenAI returned empty embedding response");
        }

        // Sort by index to ensure correct ordering
        var sortedData = result.Data.OrderBy(d => d.Index).ToArray();

        // Get total token count from API response
        var totalTokens = result.Usage?.TotalTokens;

        this._logger.LogTrace("Azure OpenAI returned {Count} embeddings, usage: {TotalTokens} tokens",
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

    /// <summary>
    /// Request body for Azure OpenAI embeddings API.
    /// </summary>
    private sealed class AzureEmbeddingRequest
    {
        [JsonPropertyName("input")]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Response from Azure OpenAI embeddings API.
    /// </summary>
    private sealed class AzureEmbeddingResponse
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
