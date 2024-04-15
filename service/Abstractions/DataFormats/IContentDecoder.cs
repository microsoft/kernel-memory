// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DataFormats;

/// <summary>
/// Interface for content decoders
/// </summary>
public interface IContentDecoder
{
    /// <summary>
    /// Returns true if the decoder supports the given MIME type
    /// </summary>
    /// <param name="mimeType">MIME type string (e.g. content type without encoding details)</param>
    /// <returns>Whether the MIME type is supported</returns>
    bool SupportsMimeType(string mimeType);

    /// <summary>
    /// Extract content from the given file.
    /// </summary>
    /// <param name="filename">Full path to the file to process</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Content extracted from the file</returns>
    Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract content from the given file.
    /// </summary>
    /// <param name="data">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Content extracted from the file</returns>
    Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract content from the given file.
    /// </summary>
    /// <param name="data">File content to process</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Content extracted from the file</returns>
    Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default);
}
