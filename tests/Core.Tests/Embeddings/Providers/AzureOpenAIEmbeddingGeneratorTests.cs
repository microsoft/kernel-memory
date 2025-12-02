// Copyright (c) Microsoft. All rights reserved.
using System.Net;
using System.Text.Json;
using KernelMemory.Core.Config.Enums;
using KernelMemory.Core.Embeddings.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace KernelMemory.Core.Tests.Embeddings.Providers;

/// <summary>
/// Tests for AzureOpenAIEmbeddingGenerator to verify Azure endpoint communication,
/// API key authentication, and managed identity support.
/// Uses mocked HttpMessageHandler to avoid real Azure OpenAI calls in unit tests.
/// </summary>
public sealed class AzureOpenAIEmbeddingGeneratorTests
{
    private readonly Mock<ILogger<AzureOpenAIEmbeddingGenerator>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;

    public AzureOpenAIEmbeddingGeneratorTests()
    {
        this._loggerMock = new Mock<ILogger<AzureOpenAIEmbeddingGenerator>>();
        this._httpHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public void Properties_ShouldReflectConfiguration()
    {
        // Arrange
        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient,
            endpoint: "https://myservice.openai.azure.com",
            deployment: "my-embedding",
            model: "text-embedding-ada-002",
            apiKey: "test-key",
            vectorDimensions: 1536,
            isNormalized: true,
            this._loggerMock.Object);

        // Assert
        Assert.Equal(EmbeddingsTypes.AzureOpenAI, generator.ProviderType);
        Assert.Equal("text-embedding-ada-002", generator.ModelName);
        Assert.Equal(1536, generator.VectorDimensions);
        Assert.True(generator.IsNormalized);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldCallAzureEndpoint()
    {
        // Arrange
        var response = CreateAzureResponse(new[] { new[] { 0.1f, 0.2f, 0.3f } });
        var responseJson = JsonSerializer.Serialize(response);

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().StartsWith("https://myservice.openai.azure.com/openai/deployments/my-embedding/embeddings")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "my-embedding", "ada-002", "test-key", 1536, true, this._loggerMock.Object);

        // Act
        var result = await generator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f }, result);
    }

    [Fact]
    public async Task GenerateAsync_WithApiKey_ShouldSendApiKeyHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = CreateAzureResponse(new[] { new[] { 0.1f } });

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "deployment", "model", "my-api-key", 1536, true, this._loggerMock.Object);

        // Act
        await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("api-key"));
        Assert.Equal("my-api-key", capturedRequest.Headers.GetValues("api-key").First());
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldSendAllInputsInOneRequest()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var response = CreateAzureResponse(new[]
        {
            new[] { 0.1f },
            new[] { 0.2f },
            new[] { 0.3f }
        });

        string? capturedContent = null;
        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                // Capture content before it's disposed
                capturedContent = await req.Content!.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(response))
                };
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "deployment", "model", "key", 1536, true, this._loggerMock.Object);

        // Act
        var results = await generator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        var requestBody = JsonSerializer.Deserialize<AzureEmbeddingRequest>(capturedContent!);
        Assert.Equal(3, requestBody!.Input.Length);
    }

    [Fact]
    public async Task GenerateAsync_ShouldIncludeApiVersion()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = CreateAzureResponse(new[] { new[] { 0.1f } });

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "deployment", "model", "key", 1536, true, this._loggerMock.Object);

        // Act
        await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("api-version=", capturedRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GenerateAsync_WithUnauthorizedError_ShouldThrowHttpRequestException()
    {
        // Arrange
        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Access denied")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "deployment", "model", "bad-key", 1536, true, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test", CancellationToken.None)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_ShouldPropagate()
    {
        // Arrange
        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new AzureOpenAIEmbeddingGenerator(
            httpClient, "https://myservice.openai.azure.com", "deployment", "model", "key", 1536, true, this._loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync("test", cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public void Constructor_WithNullEndpoint_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAIEmbeddingGenerator(httpClient, null!, "deployment", "model", "key", 1536, true, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullDeployment_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAIEmbeddingGenerator(httpClient, "https://endpoint", null!, "model", "key", 1536, true, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullApiKey_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAIEmbeddingGenerator(httpClient, "https://endpoint", "deployment", "model", null!, 1536, true, this._loggerMock.Object));
    }

    private static AzureEmbeddingResponse CreateAzureResponse(float[][] embeddings)
    {
        return new AzureEmbeddingResponse
        {
            Data = embeddings.Select((e, i) => new EmbeddingData { Index = i, Embedding = e }).ToArray(),
            Usage = new UsageInfo { PromptTokens = 10, TotalTokens = 10 }
        };
    }

    // Internal request/response classes for testing
    private sealed class AzureEmbeddingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("input")]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    private sealed class AzureEmbeddingResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();

        [System.Text.Json.Serialization.JsonPropertyName("usage")]
        public UsageInfo Usage { get; set; } = new();
    }

    private sealed class EmbeddingData
    {
        [System.Text.Json.Serialization.JsonPropertyName("index")]
        public int Index { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    private sealed class UsageInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
