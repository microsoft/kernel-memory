// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Image;

public class ImageDecoder : IContentDecoder
{
    private readonly IOcrEngine? _ocrEngine;
    private readonly ILogger<ImageDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes { get; } = [MimeTypes.ImageJpeg, MimeTypes.ImagePng, MimeTypes.ImageTiff];

    public ImageDecoder(IOcrEngine? ocrEngine = null, ILogger<ImageDecoder>? log = null)
    {
        this._ocrEngine = ocrEngine;
        this._log = log ?? DefaultLogger<ImageDecoder>.Instance;
    }

    public async Task<FileContent> ExtractContentAsync(string filename, string mimeType, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from image file {0}", filename);

        var result = new FileContent();
        var content = await this.ImageToTextAsync(filename, cancellationToken).ConfigureAwait(false);
        result.Sections.Add(new(1, content.Trim(), true));

        return result;
    }

    public async Task<FileContent> ExtractContentAsync(string name, BinaryData data, string mimeType, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from image file", name);

        var result = new FileContent();
        var content = await this.ImageToTextAsync(data, cancellationToken).ConfigureAwait(false);
        result.Sections.Add(new(1, content.Trim(), true));

        return result;
    }

    public async Task<FileContent> ExtractContentAsync(string name, Stream data, string mimeType, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from image file {0}", name);

        var result = new FileContent();
        var content = await this.ImageToTextAsync(data, cancellationToken).ConfigureAwait(false);
        result.Sections.Add(new(1, content.Trim(), true));

        return result;
    }

    private async Task<string> ImageToTextAsync(string filename, CancellationToken cancellationToken = default)
    {
        var content = File.OpenRead(filename);
        await using (content.ConfigureAwait(false))
        {
            return await this.ImageToTextAsync(content, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> ImageToTextAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        var content = data.ToStream();
        await using (content.ConfigureAwait(false))
        {
            return await this.ImageToTextAsync(content, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<string> ImageToTextAsync(Stream data, CancellationToken cancellationToken = default)
    {
        return this._ocrEngine is null
            ? throw new NotSupportedException($"Image extraction not configured")
            : this._ocrEngine.ExtractTextFromImageAsync(data, cancellationToken);
    }
}
