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
/// Tests for OpenAIEmbeddingGenerator to verify API communication, batch processing, and token count extraction.
/// Uses mocked HttpMessageHandler to avoid real OpenAI API calls in unit tests.
/// </summary>
public sealed class OpenAIEmbeddingGeneratorTests
{
    private readonly Mock<ILogger<OpenAIEmbeddingGenerator>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;

    public OpenAIEmbeddingGeneratorTests()
    {
        this._loggerMock = new Mock<ILogger<OpenAIEmbeddingGenerator>>();
        this._httpHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public void Properties_ShouldReflectConfiguration()
    {
        // Arrange
        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OpenAIEmbeddingGenerator(
            httpClient,
            apiKey: "test-key",
            model: "text-embedding-ada-002",
            vectorDimensions: 1536,
            isNormalized: true,
            baseUrl: null,
            this._loggerMock.Object);

        // Assert
        Assert.Equal(EmbeddingsTypes.OpenAI, generator.ProviderType);
        Assert.Equal("text-embedding-ada-002", generator.ModelName);
        Assert.Equal(1536, generator.VectorDimensions);
        Assert.True(generator.IsNormalized);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldCallCorrectEndpoint()
    {
        // Arrange
        var response = CreateOpenAIResponse(new[] { new[] { 0.1f, 0.2f, 0.3f } });
        var responseJson = JsonSerializer.Serialize(response);

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://api.openai.com/v1/embeddings"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "test-key", "text-embedding-ada-002", 1536, true, null, this._loggerMock.Object);

        // Act
        var result = await generator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f }, result);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldSendAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = CreateOpenAIResponse(new[] { new[] { 0.1f } });

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
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "sk-test-api-key", "text-embedding-ada-002", 1536, true, null, this._loggerMock.Object);

        // Act
        await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("sk-test-api-key", capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldSendAllInputsInOneRequest()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var response = CreateOpenAIResponse(new[]
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
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "test-key", "text-embedding-ada-002", 1536, true, null, this._loggerMock.Object);

        // Act
        var results = await generator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        var requestBody = JsonSerializer.Deserialize<OpenAIEmbeddingRequest>(capturedContent!);
        Assert.Equal(3, requestBody!.Input.Length);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomBaseUrl_ShouldUseIt()
    {
        // Arrange
        var response = CreateOpenAIResponse(new[] { new[] { 0.1f } });

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString() == "https://custom.api.com/v1/embeddings"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "test-key", "model", 1536, true, "https://custom.api.com", this._loggerMock.Object);

        // Act
        var result = await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f }, result);
    }

    [Fact]
    public async Task GenerateAsync_WithRateLimitError_ShouldThrowHttpRequestException()
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
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Rate limit exceeded")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "test-key", "model", 1536, true, null, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test", CancellationToken.None)).ConfigureAwait(false);
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
                Content = new StringContent("Invalid API key")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "bad-key", "model", 1536, true, null, this._loggerMock.Object);

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
        var generator = new OpenAIEmbeddingGenerator(
            httpClient, "test-key", "model", 1536, true, null, this._loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync("test", cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public void Constructor_WithNullApiKey_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAIEmbeddingGenerator(httpClient, null!, "model", 1536, true, null, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentException>(() =>
            new OpenAIEmbeddingGenerator(httpClient, "", "model", 1536, true, null, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullModel_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAIEmbeddingGenerator(httpClient, "key", null!, 1536, true, null, this._loggerMock.Object));
    }

    private static OpenAIEmbeddingResponse CreateOpenAIResponse(float[][] embeddings)
    {
        return new OpenAIEmbeddingResponse
        {
            Data = embeddings.Select((e, i) => new EmbeddingData { Index = i, Embedding = e }).ToArray(),
            Usage = new UsageInfo { PromptTokens = 10, TotalTokens = 10 }
        };
    }

    // Internal request/response classes for testing
    private sealed class OpenAIEmbeddingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("input")]
        public string[] Input { get; set; } = Array.Empty<string>();
    }

    private sealed class OpenAIEmbeddingResponse
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
