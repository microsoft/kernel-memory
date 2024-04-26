// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.AI.Anthropic;

/// <summary>
/// Configuration for Text Generation with Anthropic
/// </summary>
public class AnthropicConfiguration
{
    /// <summary>
    /// Anthropic web service endpoint
    /// </summary>
    public string Endpoint { get; set; } = "https://api.anthropic.com";

    /// <summary>
    /// Anthropic endpoint version
    /// </summary>
    public string EndpointVersion { get; set; } = "2023-06-01";

    /// <summary>
    /// Api key needed to access the Anthropic API
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the model to use for text generation
    /// See https://docs.anthropic.com/claude/docs/models-overview
    ///
    /// Opus: most powerful model.
    /// Sonnet: most balanced model between intelligence and speed.
    /// Haiku: fastest and most compact model.
    /// </summary>
    public string TextModelName { get; set; } = "claude-3-sonnet-20240229";

    /// <summary>
    /// This allows configuring the maximum token total that can be generated.
    /// Default is 200k.
    /// See https://docs.anthropic.com/claude/docs/models-overview
    /// </summary>
    public int MaxTokenIn { get; set; } = 200_000;

    /// <summary>
    /// This allows configuring the maximum token total that can be generated.
    /// Default is 4096.
    /// See https://docs.anthropic.com/claude/docs/models-overview
    /// </summary>
    public int MaxTokenOut { get; set; } = 4096;

    /// <summary>
    /// This allows configuring the client that will be used for httpclient factory to create client.
    /// It can be left empty if the default client is to be used.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// Validate the configuration
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ConfigurationException("The API Key is empty");
        }
    }
}
