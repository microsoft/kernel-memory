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

    public IEnumerable<string> SupportedMimeTypes { get; } = new[] { MimeTypes.Html };

    public HtmlDecoder(ILogger<HtmlDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<HtmlDecoder>.Instance;
    }

    public Task<FileContent> ExtractContentAsync(string filename, string mimeType, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(Path.GetFileName(filename), stream, mimeType, cancellationToken);
    }

    public Task<FileContent> ExtractContentAsync(string name, BinaryData data, string mimeType, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.ExtractContentAsync(name, stream, mimeType, cancellationToken);
    }

    public Task<FileContent> ExtractContentAsync(string name, Stream data, string mimeType, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from HTML file {0}", name);

        var doc = new HtmlDocument();
        doc.Load(data);

        var result = new FileContent();
        if (mimeType == MimeTypes.MarkDown)
        {
            result.MimeType = MimeTypes.MarkDown;
        }

        result.Sections.Add(new FileSection(1, doc.DocumentNode.InnerText.Trim(), true));

        return Task.FromResult(result);
    }
}
