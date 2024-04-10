// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Text;

public class TextDecoder : IContentDecoder
{
    private readonly ILogger<TextDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes { get; } = [MimeTypes.PlainText, MimeTypes.MarkDown, MimeTypes.Json];

    public TextDecoder(ILogger<TextDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<TextDecoder>.Instance;
    }

    public Task<FileContent?> ExtractContentAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(Path.GetFileName(filename), stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string name, BinaryData data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from text file {0}", name);

        var result = new FileContent();
        result.Sections.Add(new(1, data.ToString().Trim(), true));

        return Task.FromResult(result)!;
    }

    public async Task<FileContent?> ExtractContentAsync(string name, Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from text file {0}", name);

        var result = new FileContent();

        using var reader = new StreamReader(data);
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);

        result.Sections.Add(new(1, content.Trim(), true));

        return result;
    }
}
