// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Text;

[Experimental("KMEXP00")]
public sealed class TextDecoder : IContentDecoder
{
    private readonly ILogger<TextDecoder> _log;

    public TextDecoder(ILogger<TextDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<TextDecoder>.Instance;
    }

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && (
            mimeType.StartsWith(MimeTypes.PlainText, StringComparison.OrdinalIgnoreCase) ||
            mimeType.StartsWith(MimeTypes.Json, StringComparison.OrdinalIgnoreCase)
        );
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
        this._log.LogDebug("Extracting text from file");

        var result = new FileContent(MimeTypes.PlainText);
        result.Sections.Add(new(1, data.ToString().Trim(), true));

        return Task.FromResult(result)!;
    }

    /// <inheritdoc />
    public async Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from file");

        var result = new FileContent(MimeTypes.PlainText);
        using var reader = new StreamReader(data);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        result.Sections.Add(new(1, content.Trim(), true));
        return result;
    }
}
