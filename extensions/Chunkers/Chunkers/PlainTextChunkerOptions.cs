// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.Chunkers;

public class PlainTextChunkerOptions
{
    private int _maxTokensPerChunk = 1024;
    private int _overlap = 0;

    /// <summary>
    /// Maximum number of tokens per chunk (must be > 0)
    /// </summary>
    public int MaxTokensPerChunk
    {
        get => this._maxTokensPerChunk;
        set => this._maxTokensPerChunk = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(this.MaxTokensPerChunk), "Value must be greater than zero.");
    }

    /// <summary>
    /// Number of tokens to copy and repeat from a chunk into the next (must be >= 0)
    /// </summary>
    public int Overlap
    {
        get => this._overlap;
        set => this._overlap = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(this.Overlap), "Value must be zero or greater.");
    }

    /// <summary>
    /// Optional header to add before each chunk.
    /// </summary>
    public string? ChunkHeader { get; set; } = null;
}
