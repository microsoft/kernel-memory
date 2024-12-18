﻿// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

/// <summary>
/// OpenAI settings.
/// </summary>
public class OpenAIConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TextGenerationTypes
    {
        Auto = 0,
        TextCompletion,
        Chat,
    }

    /// <summary>
    /// The type of OpenAI completion to use, either Text (legacy) or Chat.
    /// When using Auto, the client uses OpenAI model names to detect the correct protocol.
    /// Most OpenAI models use Chat. If you're using a non-OpenAI model, you might want to set this manually.
    /// </summary>
    public TextGenerationTypes TextGenerationType { get; set; } = TextGenerationTypes.Auto;

    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional OpenAI Organization ID.
    /// </summary>
    public string? OrgId { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI HTTP endpoint. You may need to override this to work with
    /// OpenAI compatible services like LM Studio.
    /// </summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Model used for text generation. Chat models can be used too.
    /// </summary>
    public string TextModel { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by the text model.
    /// </summary>
    public int TextModelMaxTokenTotal { get; set; } = 8192;

    /// <summary>
    /// Name of the tokenizer used to count tokens.
    /// Supported values: "p50k", "cl100k", "o200k". Leave it empty for autodetect.
    /// </summary>
    public string TextModelTokenizer { get; set; } = string.Empty;

    /// <summary>
    /// Model used to embedding generation.
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by the embedding model.
    /// Default to OpenAI ADA2 settings.
    /// </summary>
    public int EmbeddingModelMaxTokenTotal { get; set; } = 8191;

    /// <summary>
    /// Name of the tokenizer used to count tokens.
    /// Supported values: "p50k", "cl100k", "o200k". Leave it empty for autodetect.
    /// </summary>
    public string EmbeddingModelTokenizer { get; set; } = string.Empty;

    /// <summary>
    /// The number of dimensions output embeddings should have.
    /// Only supported in "text-embedding-3" and later models developed with
    /// MRL, see https://arxiv.org/abs/2205.13147
    /// </summary>
    public int? EmbeddingDimensions { get; set; }

    /// <summary>
    /// Per documentation the max value is 2048, however, the default is lower (100)
    /// to avoid sending too many tokens and being throttled.
    ///
    /// You can increase the value in your local configuration if needed.
    ///
    /// See https://platform.openai.com/docs/api-reference/embeddings/create.
    /// </summary>
    public int MaxEmbeddingBatchSize { get; set; } = 100;

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
            throw new ConfigurationException($"OpenAI: {nameof(this.APIKey)} is empty");
        }

        if (this.TextModelMaxTokenTotal < 1)
        {
            throw new ConfigurationException($"OpenAI: {nameof(this.TextModelMaxTokenTotal)} cannot be less than 1");
        }

        if (this.EmbeddingModelMaxTokenTotal < 1)
        {
            throw new ConfigurationException($"OpenAI: {nameof(this.EmbeddingModelMaxTokenTotal)} cannot be less than 1");
        }

        if (this.EmbeddingDimensions is < 1)
        {
            throw new ConfigurationException($"OpenAI: {nameof(this.EmbeddingDimensions)} cannot be less than 1");
        }
    }
}
