// Copyright (c) Microsoft. All rights reserved.

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
}
