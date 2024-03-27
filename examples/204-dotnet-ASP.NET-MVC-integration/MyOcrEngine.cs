// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;

public class MyOcrEngine : IOcrEngine
{
    public Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("test");
    }
}
