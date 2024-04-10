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

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from {0} file {1}", file.MimeType, file.Name);

        var result = new FileContent();
        result.Sections.Add(new(1, data.ToString().Trim(), true));

        return Task.FromResult(result)!;
    }

    public async Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text from {0} file {1}", file.MimeType, file.Name);

        var result = new FileContent();

        using var reader = new StreamReader(data);
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);

        result.Sections.Add(new(1, content.Trim(), true));

        return result;
    }
}
