// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Polly;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public class WebScraper
{
    public class Result
    {
        public bool Success { get; set; } = false;
        public string Error { get; set; } = string.Empty;
        public BinaryData Content { get; set; } = new(string.Empty);
        public string ContentType { get; set; } = string.Empty;
    }

    private readonly ILogger _log;

    public WebScraper(ILogger? log = null)
    {
        this._log = log ?? DefaultLogger<WebScraper>.Instance;
    }

    public async Task<Result> GetTextAsync(string url, CancellationToken cancellationToken = default)
    {
        return await this.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> GetAsync(Uri url, CancellationToken cancellationToken = default)
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
            .ExecuteAsync(async _ => await client.GetAsync(url, cancellationToken).ConfigureAwait(false), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            this._log.LogError("Error while fetching page {0}, status code: {1}", url.AbsoluteUri, response.StatusCode);
            return new Result { Success = false, Error = $"HTTP error, status code: {response.StatusCode}" };
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        this._log.LogDebug("'{0}' content type: {1}", url.AbsoluteUri, contentType);

        var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = new Result { Success = true, Content = new BinaryData(content), ContentType = string.Empty };

        // TODO: refactor to automatically support all mime types known by KM
        if (contentType.Contains(MimeTypes.PlainText, StringComparison.OrdinalIgnoreCase))
        {
            // Most web servers, e.g. GitHub, return "text/plain" also for markdown files
            result.ContentType = url.AbsolutePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? MimeTypes.MarkDown
                : MimeTypes.PlainText;
        }
        else if (contentType.Contains(MimeTypes.MarkDown, StringComparison.OrdinalIgnoreCase)
                 || contentType.Contains(MimeTypes.MarkDownOld1, StringComparison.OrdinalIgnoreCase)
                 || contentType.Contains(MimeTypes.MarkDownOld2, StringComparison.OrdinalIgnoreCase))
        {
            result.ContentType = MimeTypes.MarkDown;
        }
        else if (contentType.Contains(MimeTypes.Html, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.Html; }
        else if (contentType.Contains(MimeTypes.MsWord, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsWord; }
        else if (contentType.Contains(MimeTypes.MsWordX, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsWordX; }
        else if (contentType.Contains(MimeTypes.MsPowerPoint, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsPowerPoint; }
        else if (contentType.Contains(MimeTypes.MsPowerPointX, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsPowerPointX; }
        else if (contentType.Contains(MimeTypes.MsExcel, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsExcel; }
        else if (contentType.Contains(MimeTypes.MsExcelX, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.MsExcelX; }
        else if (contentType.Contains(MimeTypes.Pdf, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.Pdf; }
        else if (contentType.Contains(MimeTypes.Json, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.Json; }
        else if (contentType.Contains(MimeTypes.ImageBmp, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.ImageBmp; }
        else if (contentType.Contains(MimeTypes.ImageGif, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.ImageGif; }
        else if (contentType.Contains(MimeTypes.ImageJpeg, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.ImageJpeg; }
        else if (contentType.Contains(MimeTypes.ImagePng, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.ImagePng; }
        else if (contentType.Contains(MimeTypes.ImageTiff, StringComparison.OrdinalIgnoreCase)) { result.ContentType = MimeTypes.ImageTiff; }

        return string.IsNullOrEmpty(result.ContentType)
            ? new Result { Success = false, Error = $"Unsupported content type: {contentType}" }
            : result;
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
