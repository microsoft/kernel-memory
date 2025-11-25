// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DataFormats;

/// <summary>
/// An OCR engine that can read in text from image files.
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Reads all text from the image.
    /// </summary>
    /// <param name="imageContent">The image content stream.</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default);
}
