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
/// Tests for OllamaEmbeddingGenerator to verify HTTP communication, response parsing, and error handling.
/// Uses mocked HttpMessageHandler to avoid real Ollama calls in unit tests.
/// </summary>
public sealed class OllamaEmbeddingGeneratorTests
{
    private readonly Mock<ILogger<OllamaEmbeddingGenerator>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;

    public OllamaEmbeddingGeneratorTests()
    {
        this._loggerMock = new Mock<ILogger<OllamaEmbeddingGenerator>>();
        this._httpHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public void Properties_ShouldReflectConfiguration()
    {
        // Arrange
        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OllamaEmbeddingGenerator(
            httpClient,
            baseUrl: "http://localhost:11434",
            model: "qwen3-embedding",
            vectorDimensions: 1024,
            isNormalized: true,
            this._loggerMock.Object);

        // Assert
        Assert.Equal(EmbeddingsTypes.Ollama, generator.ProviderType);
        Assert.Equal("qwen3-embedding", generator.ModelName);
        Assert.Equal(1024, generator.VectorDimensions);
        Assert.True(generator.IsNormalized);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldCallCorrectEndpoint()
    {
        // Arrange
        var response = new OllamaEmbeddingResponse { Embedding = new[] { 0.1f, 0.2f, 0.3f } };
        var responseJson = JsonSerializer.Serialize(response);

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://localhost:11434/api/embeddings"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        // Act
        var result = await generator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f }, result);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldSendCorrectRequestBody()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = new OllamaEmbeddingResponse { Embedding = new[] { 0.1f } };

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
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        // Act
        await generator.GenerateAsync("hello world", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedRequest);
        var content = await capturedRequest!.Content!.ReadAsStringAsync().ConfigureAwait(false);
        var requestBody = JsonSerializer.Deserialize<OllamaEmbeddingRequest>(content);
        Assert.Equal("qwen3-embedding", requestBody!.Model);
        Assert.Equal("hello world", requestBody.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldProcessAllTexts()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var vectors = new[]
        {
            new[] { 0.1f },
            new[] { 0.2f },
            new[] { 0.3f }
        };

        var callIndex = 0;
        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new OllamaEmbeddingResponse { Embedding = vectors[callIndex++] };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(response))
                };
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        // Act
        var results = await generator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.Equal(new[] { 0.1f }, results[0]);
        Assert.Equal(new[] { 0.2f }, results[1]);
        Assert.Equal(new[] { 0.3f }, results[2]);
    }

    [Fact]
    public async Task GenerateAsync_WithHttpError_ShouldThrowHttpRequestException()
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
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test", CancellationToken.None)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateAsync_WithMalformedResponse_ShouldThrowJsonException()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("not json")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(
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
        var generator = new OllamaEmbeddingGenerator(
            httpClient, "http://localhost:11434", "qwen3-embedding", 1024, true, this._loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync("test", cts.Token)).ConfigureAwait(false);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrow()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OllamaEmbeddingGenerator(null!, "http://localhost", "model", 1024, true, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullBaseUrl_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OllamaEmbeddingGenerator(httpClient, null!, "model", 1024, true, this._loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullModel_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OllamaEmbeddingGenerator(httpClient, "http://localhost", null!, 1024, true, this._loggerMock.Object));
    }

    // Internal request/response classes for testing
    private sealed class OllamaEmbeddingRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
