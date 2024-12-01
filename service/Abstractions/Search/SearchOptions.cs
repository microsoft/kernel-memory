// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

// TODO: move minRelevance to this class
// TODO: move filter to this class
// TODO: move filters to this class
public sealed class SearchOptions
{
    /// <summary>
    /// Whether to stream results back to the client
    /// </summary>
    public bool Stream { get; set; } = false;
}

public static class SearchOptionsExtensions
{
    public static SearchOptions? Clone(this SearchOptions? options)
    {
        if (options == null) { return null; }

        return new SearchOptions
        {
            Stream = options.Stream
        };
    }
}
