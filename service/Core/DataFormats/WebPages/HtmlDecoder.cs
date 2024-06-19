// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

[Experimental("KMEXP00")]
public sealed class HtmlDecoder : IContentDecoder
{
    private readonly ILogger<HtmlDecoder> _log;

    public HtmlDecoder(ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<HtmlDecoder>();
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.Html, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from HTML file");

        var result = new FileContent(MimeTypes.PlainText);
        var doc = new HtmlDocument();
        doc.Load(data);

        result.Sections.Add(new FileSection(1, doc.DocumentNode.InnerText.Trim(), true));

        return Task.FromResult(result);
    }
}
