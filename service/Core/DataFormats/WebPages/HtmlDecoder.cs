// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public class HtmlDecoder : IContentDecoder
{
    private readonly ILogger<HtmlDecoder> _log;

    /// <inheritdoc />
    public IEnumerable<string> SupportedMimeTypes { get; } = new[] { MimeTypes.Html };

    public HtmlDecoder(ILogger<HtmlDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<HtmlDecoder>.Instance;
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

        var result = new FileContent();

        var doc = new HtmlDocument();
        doc.Load(data);

        result.Sections.Add(new FileSection(1, doc.DocumentNode.InnerText.Trim(), true));

        return Task.FromResult(result);
    }
}
