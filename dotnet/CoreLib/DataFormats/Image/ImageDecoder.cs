// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.DataFormats.Image;

public class ImageDecoder
{
    public Task<string> ImageToTextAsync(IOcrEngine engine, string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.ImageToTextAsync(engine, stream);
    }

    public Task<string> ImageToTextAsync(IOcrEngine engine, BinaryData data)
    {
        using var stream = data.ToStream();
        return this.ImageToTextAsync(engine, stream);
    }

    public Task<string> ImageToTextAsync(IOcrEngine engine, Stream data)
    {
        return engine.ExtractTextFromImageAsync(data);
    }
}
