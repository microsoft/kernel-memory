// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using Polly;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public class WebScraperDecoder : IContentDecoder
{
    public class Result
    {
        public bool Success { get; set; } = false;
        public string Text { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    private readonly ILogger<WebScraperDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes { get; } = [MimeTypes.WebPageUrl];

    public WebScraperDecoder(ILogger<WebScraperDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<WebScraperDecoder>.Instance;
    }

    public async Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default)
    {
        // In case of WebScraper, the filename is the URL of the web page.
        this._log.LogDebug("Downloading web page specified in {0} and extracting text from {1}", file.Name, filename);
        if (string.IsNullOrWhiteSpace(filename))
        {
            file.Log(handlerStepName, "The web page URL is empty");
            this._log.LogWarning("The web page URL is empty");

            return null;
        }

        var result = await this.GetTextAsync(filename, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            file.Log(handlerStepName, $"Download error: {result.Error}");
            this._log.LogWarning("Web page download error: {0}", result.Error);

            return null;
        }

        if (string.IsNullOrEmpty(result.Text))
        {
            file.Log(handlerStepName, "The web page has no text content, skipping it");
            this._log.LogWarning("The web page has no text content, skipping it");

            return null;
        }

        var content = new FileContent();
        content.Sections.Add(new(1, result.Text.Trim(), true));
        this._log.LogDebug("Web page {0} downloaded, text length: {1}", filename, result.Text.Length);

        return content;
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default)
    {
        return this.ExtractContentAsync(handlerStepName, file, data.ToString(), cancellationToken);
    }

    public async Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data);
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);

        return await this.ExtractContentAsync(handlerStepName, file, content, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> GetTextAsync(string url, CancellationToken cancellationToken = default)
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
            .ExecuteAsync(async cancellationToken => await client.GetAsync(url, cancellationToken).ConfigureAwait(false), cancellationToken)
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
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new Result { Success = true, Text = content.Trim() };
        }

        if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var doc = new HtmlDocument();
            Stream content = new MemoryStream(Encoding.UTF8.GetBytes(html));
            await using (content.ConfigureAwait(false))
            {
                doc.Load(content);
            }

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
