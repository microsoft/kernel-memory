// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using Microsoft.Extensions.Logging;

namespace KernelMemory.Core.Http;

/// <summary>
/// Simple HTTP retry policy with exponential backoff for transient failures (e.g., 429/503).
/// </summary>
public static class HttpRetryPolicy
{
    /// <summary>
    /// Sends an HTTP request using a request factory, retrying on transient failures.
    /// The request factory must create a new <see cref="HttpRequestMessage"/> each time (requests are single-use).
    /// </summary>
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> requestFactory,
        ILogger logger,
        CancellationToken ct,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(requestFactory, nameof(requestFactory));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        delayAsync ??= Task.Delay;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= Constants.HttpRetryDefaults.MaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            using var request = requestFactory();

            try
            {
                var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                if (!IsRetryableStatusCode(response.StatusCode) || attempt == Constants.HttpRetryDefaults.MaxAttempts)
                {
                    return response;
                }

                var delay = CalculateDelay(attempt, response);
                logger.LogWarning(
                    "HTTP call failed with {StatusCode}. Retrying attempt {Attempt}/{MaxAttempts} after {DelayMs}ms",
                    (int)response.StatusCode,
                    attempt + 1,
                    Constants.HttpRetryDefaults.MaxAttempts,
                    (int)delay.TotalMilliseconds);

                response.Dispose();
                await delayAsync(delay, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                lastException = ex;
                if (attempt == Constants.HttpRetryDefaults.MaxAttempts)
                {
                    throw;
                }

                var delay = CalculateDelay(attempt, response: null);
                logger.LogWarning(
                    ex,
                    "HTTP call timed out. Retrying attempt {Attempt}/{MaxAttempts} after {DelayMs}ms",
                    attempt + 1,
                    Constants.HttpRetryDefaults.MaxAttempts,
                    (int)delay.TotalMilliseconds);

                await delayAsync(delay, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt == Constants.HttpRetryDefaults.MaxAttempts)
                {
                    throw;
                }

                var delay = CalculateDelay(attempt, response: null);
                logger.LogWarning(
                    ex,
                    "HTTP call failed with exception. Retrying attempt {Attempt}/{MaxAttempts} after {DelayMs}ms",
                    attempt + 1,
                    Constants.HttpRetryDefaults.MaxAttempts,
                    (int)delay.TotalMilliseconds);

                await delayAsync(delay, ct).ConfigureAwait(false);
            }
        }

        throw lastException ?? new HttpRequestException("HTTP call failed after retries");
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests ||
               statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.InternalServerError ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan CalculateDelay(int attempt, HttpResponseMessage? response)
    {
        var retryAfter = response != null ? TryGetRetryAfterDelay(response) : null;
        if (retryAfter.HasValue)
        {
            return ClampDelay(retryAfter.Value);
        }

        var exponentialMs = Constants.HttpRetryDefaults.BaseDelayMs * Math.Pow(2, attempt - 1);
        var clampedMs = Math.Min(exponentialMs, Constants.HttpRetryDefaults.MaxDelayMs);

        // Deterministic jitter (avoid Random in deterministic tests/builds)
        var jitterMs = (attempt * 37) % 101;
        return TimeSpan.FromMilliseconds(Math.Min(clampedMs + jitterMs, Constants.HttpRetryDefaults.MaxDelayMs));
    }

    private static TimeSpan ClampDelay(TimeSpan delay)
    {
        var ms = Math.Clamp(delay.TotalMilliseconds, 0, Constants.HttpRetryDefaults.MaxDelayMs);
        return TimeSpan.FromMilliseconds(ms);
    }

    private static TimeSpan? TryGetRetryAfterDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta != null)
        {
            return response.Headers.RetryAfter.Delta.Value;
        }

        if (response.Headers.RetryAfter?.Date != null)
        {
            var delta = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        return null;
    }
}
