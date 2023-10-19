// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;
using Polly;

namespace Microsoft.SemanticMemory.DataFormats.WebPages;

public class WebScraper
{
    public class Result
    {
        public bool Success { get; set; } = false;
        public string Text { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    private readonly ILogger _log;

    public WebScraper(ILogger? log = null)
    {
        this._log = log ?? DefaultLogger<WebScraper>.Instance;
    }

    public async Task<Result> GetTextAsync(string url)
    {
        return await this.GetAsync(new Uri(url)).ConfigureAwait(false);
    }

    private async Task<Result> GetAsync(Uri url)
    {
        var scheme = url.Scheme.ToUpperInvariant();
        if ((scheme != "HTTP") && (scheme != "HTTPS"))
        {
            return new Result { Success = false, Error = $"Unknown URL protocol: {url.Scheme}" };
        }

        // TODO: perf/TCP ports/reuse client
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(Telemetry.HttpUserAgent);
        HttpResponseMessage? response = await RetryLogic()
            .ExecuteAsync(async cancellationToken => await client.GetAsync(url, cancellationToken).ConfigureAwait(false))
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            this._log.LogError("Error while fetching page {0}, status code: {1}", url.AbsoluteUri, response.StatusCode);
            return new Result { Success = false, Error = $"HTTP error, status code: {response.StatusCode}" };
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        this._log.LogDebug("{0} content type: {1}", url.AbsoluteUri, contentType);

        if (contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new Result { Success = true, Text = content.Trim() };
        }

        if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = new HtmlDocument();
            using Stream content = new MemoryStream(Encoding.UTF8.GetBytes(html));
            doc.Load(content);

            return new Result { Success = true, Text = doc.DocumentNode.InnerText.Trim() };
        }

        // TODO: download and extract
        // if (contentType.Contains("application/pdf", StringComparison.OrdinalIgnoreCase))
        // {
        //     return new Result { Success = false, Error = $"Invalid content type: {contentType}" };
        // }

        // TODO: download and extract
        // if (contentType.Contains("application/msword", StringComparison.OrdinalIgnoreCase))
        // {
        //     return new Result { Success = false, Error = $"Invalid content type: {contentType}" };
        // }

        return new Result { Success = false, Error = $"Invalid content type: {contentType}" };
    }

    private static ResiliencePipeline<HttpResponseMessage> RetryLogic()
    {
        var retriableErrors = new[]
        {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.GatewayTimeout, // 504
        };

        const int MaxDelay = 5;
        var delays = new List<int> { 1, 1, 1, 2, 2, 3, 4, MaxDelay };

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(resp => retriableErrors.Contains(resp.StatusCode)),
                MaxRetryAttempts = 10,
                DelayGenerator = args =>
                {
                    double secs = (args.AttemptNumber < delays.Count) ? delays[args.AttemptNumber] : MaxDelay;
                    return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(secs));
                }
            })
            .Build();
    }
}
