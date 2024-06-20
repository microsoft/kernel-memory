// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Microsoft.KernelMemory.DataFormats.Pdf;

[Experimental("KMEXP00")]
public sealed class PdfDecoder : IContentDecoder
{
    private readonly ILogger<PdfDecoder> _log;

    public PdfDecoder(ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<PdfDecoder>();
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.Pdf, StringComparison.OrdinalIgnoreCase);
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
        this._log.LogDebug("Extracting text from PDF file");

        var result = new FileContent(MimeTypes.PlainText);
        using PdfDocument? pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return Task.FromResult(result); }

        foreach (Page? page in pdfDocument.GetPages().Where(x => x != null))
        {
            // Note: no trimming, use original spacing
            string pageContent = ContentOrderTextExtractor.GetText(page) ?? string.Empty;
            result.Sections.Add(new FileSection(page.Number, pageContent, false));
        }

        return Task.FromResult(result);
    }
}
