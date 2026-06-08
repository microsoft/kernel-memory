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
/// Tests for HuggingFaceEmbeddingGenerator to verify HF Inference API communication,
/// authentication via HF_TOKEN, and response parsing.
/// Uses mocked HttpMessageHandler to avoid real HuggingFace API calls in unit tests.
/// </summary>
public sealed class HuggingFaceEmbeddingGeneratorTests
{
    private readonly Mock<ILogger<HuggingFaceEmbeddingGenerator>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;

    public HuggingFaceEmbeddingGeneratorTests()
    {
        this._loggerMock = new Mock<ILogger<HuggingFaceEmbeddingGenerator>>();
        this._httpHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public void Properties_ShouldReflectConfiguration()
    {
        // Arrange
        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient,
            apiKey: "hf_test_token",
            model: "sentence-transformers/all-MiniLM-L6-v2",
            vectorDimensions: 384,
            isNormalized: true,
            baseUrl: null,
            this._loggerMock.Object,
            batchSize: 10);

        // Assert
        Assert.Equal(EmbeddingsTypes.HuggingFace, generator.ProviderType);
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", generator.ModelName);
        Assert.Equal(384, generator.VectorDimensions);
        Assert.True(generator.IsNormalized);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldCallCorrectEndpoint()
    {
        // Arrange
        var response = new[] { new[] { 0.1f, 0.2f, 0.3f } };
        var responseJson = JsonSerializer.Serialize(response);

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://api-inference.huggingface.co/models/sentence-transformers/all-MiniLM-L6-v2"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "sentence-transformers/all-MiniLM-L6-v2", 384, true, null, this._loggerMock.Object, batchSize: 10);

        // Act
        var result = await generator.GenerateAsync("test text", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f, 0.2f, 0.3f }, result.Vector);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldSendAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = new[] { new[] { 0.1f } };

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
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_my_secret_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

        // Act
        await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("hf_my_secret_token", capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GenerateAsync_Single_ShouldSendCorrectRequestBody()
    {
        // Arrange
        string? capturedContent = null;
        var response = new[] { new[] { 0.1f } };

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
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

        // Act
        await generator.GenerateAsync("hello world", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(capturedContent);
        var requestBody = JsonSerializer.Deserialize<HuggingFaceRequest>(capturedContent!);
        Assert.NotNull(requestBody);
        Assert.Contains("hello world", requestBody!.Inputs);
    }

    [Fact]
    public async Task GenerateAsync_Batch_ShouldProcessAllTexts()
    {
        // Arrange
        var texts = new[] { "text1", "text2", "text3" };
        var response = new[]
        {
            new[] { 0.1f },
            new[] { 0.2f },
            new[] { 0.3f }
        };

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

        // Act
        var results = await generator.GenerateAsync(texts, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.Equal(new[] { 0.1f }, results[0].Vector);
        Assert.Equal(new[] { 0.2f }, results[1].Vector);
        Assert.Equal(new[] { 0.3f }, results[2].Vector);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomBaseUrl_ShouldUseIt()
    {
        // Arrange
        var response = new[] { new[] { 0.1f } };

        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().StartsWith("https://custom.hf-endpoint.com")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "model", 384, true, "https://custom.hf-endpoint.com", this._loggerMock.Object, batchSize: 10);

        // Act
        var result = await generator.GenerateAsync("test", CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.Equal(new[] { 0.1f }, result.Vector);
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
                Content = new StringContent("Invalid token")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "bad_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test", CancellationToken.None)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateAsync_WithModelLoadingError_ShouldThrowHttpRequestException()
    {
        // Arrange - HuggingFace returns 503 when model is loading
        this._httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("{\"error\":\"Model is currently loading\"}")
            });

        var httpClient = new HttpClient(this._httpHandlerMock.Object);
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

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
        var generator = new HuggingFaceEmbeddingGenerator(
            httpClient, "hf_token", "model", 384, true, null, this._loggerMock.Object, batchSize: 10);

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
            new HuggingFaceEmbeddingGenerator(httpClient, null!, "model", 384, true, null, this._loggerMock.Object, batchSize: 10));
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentException>(() =>
            new HuggingFaceEmbeddingGenerator(httpClient, "", "model", 384, true, null, this._loggerMock.Object, batchSize: 10));
    }

    [Fact]
    public void Constructor_WithNullModel_ShouldThrow()
    {
        // Assert
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new HuggingFaceEmbeddingGenerator(httpClient, "key", null!, 384, true, null, this._loggerMock.Object, batchSize: 10));
    }

    // Internal request class for testing
    private sealed class HuggingFaceRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("inputs")]
        public string[] Inputs { get; set; } = Array.Empty<string>();
    }
}
