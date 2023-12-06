// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DataFormats.Image;

public class ImageDecoder
{
    public async Task<string> ImageToTextAsync(IOcrEngine engine, string filename, CancellationToken cancellationToken = default)
    {
        var content = File.OpenRead(filename);
        await using (content.ConfigureAwait(false))
        {
            return await this.ImageToTextAsync(engine, content, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> ImageToTextAsync(IOcrEngine engine, BinaryData data, CancellationToken cancellationToken = default)
    {
        var content = data.ToStream();
        await using (content.ConfigureAwait(false))
        {
            return await this.ImageToTextAsync(engine, content, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<string> ImageToTextAsync(IOcrEngine engine, Stream data, CancellationToken cancellationToken = default)
    {
        return engine.ExtractTextFromImageAsync(data, cancellationToken);
    }
}
