// Copyright (c) Microsoft. All rights reserved.

using System;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// OpenAI settings.
/// </summary>
public class OpenAIConfig
{
    /// <summary>
    /// Model used for text generation. Chat models can be used too.
    /// </summary>
    public string TextModel { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by the text model.
    /// </summary>
    public int TextModelMaxTokenTotal { get; set; } = 8192;

    /// <summary>
    /// Model used to embedding generation/
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by the embedding model.
    /// Default to OpenAI ADA2 settings.
    /// </summary>
    public int EmbeddingModelMaxTokenTotal { get; set; } = 8191;

    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional OpenAI Organization ID.
    /// </summary>
    public string? OrgId { get; set; } = string.Empty;

    /// <summary>
    /// How many times to retry in case of throttling.
    /// </summary>
    public int MaxRetries { get; set; } = 10;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.APIKey))
        {
            throw new ArgumentOutOfRangeException(nameof(this.APIKey), "The API Key is empty");
        }

        if (this.TextModelMaxTokenTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.TextModelMaxTokenTotal),
                $"{nameof(this.TextModelMaxTokenTotal)} cannot be less than 1");
        }

        if (this.EmbeddingModelMaxTokenTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.EmbeddingModelMaxTokenTotal),
                $"{nameof(this.EmbeddingModelMaxTokenTotal)} cannot be less than 1");
        }
    }
}
