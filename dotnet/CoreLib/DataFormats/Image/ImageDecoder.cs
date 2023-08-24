// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.DataFormats.Image;

public class ImageDecoder
{
    public async Task<string> ImageToTextAsync(IOcrEngine engine, string filename)
    {
        using var stream = File.OpenRead(filename);
        return await this.ImageToTextAsync(engine, stream).ConfigureAwait(false);
    }

    public async Task<string> ImageToTextAsync(IOcrEngine engine, BinaryData data)
    {
        using var stream = data.ToStream();
        return await this.ImageToTextAsync(engine, stream).ConfigureAwait(false);
    }

    public Task<string> ImageToTextAsync(IOcrEngine engine, Stream data)
    {
        return engine.ExtractTextFromImageAsync(data);
    }
}
