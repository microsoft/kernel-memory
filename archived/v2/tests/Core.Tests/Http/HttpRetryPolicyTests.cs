// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using KernelMemory.Core.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace KernelMemory.Core.Tests.Http;

public sealed class HttpRetryPolicyTests
{
    [Fact]
    public async Task SendAsync_With429ThenSuccess_ShouldRetryAndReturnSuccessResponse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var loggerMock = new Mock<ILogger>();
        var delayCalls = new List<TimeSpan>();

        var callIndex = 0;
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callIndex++;
                return callIndex < 3
                    ? new HttpResponseMessage { StatusCode = HttpStatusCode.TooManyRequests, Content = new StringContent("rate limit") }
                    : new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("ok") };
            });

        using var httpClient = new HttpClient(handlerMock.Object);

        // Act
        using var response = await HttpRetryPolicy.SendAsync(
            httpClient,
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            loggerMock.Object,
            CancellationToken.None,
            delayAsync: (d, _) =>
            {
                delayCalls.Add(d);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, callIndex);
        Assert.Equal(2, delayCalls.Count);
    }
}
