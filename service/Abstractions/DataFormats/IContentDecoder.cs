// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DataFormats;

public interface IContentDecoder
{
    IEnumerable<string> SupportedMimeTypes { get; }

    Task<FileContent> ExtractContentAsync(string filename, string mimeType, CancellationToken cancellationToken = default);

    Task<FileContent> ExtractContentAsync(string name, BinaryData data, string mimeType, CancellationToken cancellationToken = default);

    Task<FileContent> ExtractContentAsync(string name, Stream data, string mimeType, CancellationToken cancellationToken = default);
}
