// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats;

public interface IContentDecoder
{
    IEnumerable<string> SupportedMimeTypes { get; }

    Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default);

    Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default);

    Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default);
}
