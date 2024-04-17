// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.KernelMemory.AI.Anthropic;

/// <summary>
/// Configuration for Text Generation with Anthropic
/// </summary>
public class AnthropicConfiguration
{
    /// <summary>
    /// Haiku model
    /// </summary>
    public const string HaikuModelName = "claude-3-haiku-20240307";

    /// <summary>
    /// Sonnet model
    /// </summary>
    public const string SonnetModelName = "claude-3-sonnet-20240229";

    /// <summary>
    /// Opus model
    /// </summary>
    public const string OpusModelName = "claude-3-opus-20240229";

    /// <summary>
    /// This allows configuring the client that will be used for httpclient factory to create client.
    /// It can be left empty if the default client is to be used.
    /// </summary>
    public string? HttpClientName { get; set; }

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
    /// This allows configuring the maximum token total that can be generated. Default is 2048.
    /// </summary>
    public int MaxTokenTotal { get; set; } = 2048;

    /// <summary>
    /// Name of the model to use for text generation
    /// </summary>
    public string TextModelName { get; set; } = HaikuModelName;

    /// <summary>
    /// Validate the configuration
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ArgumentOutOfRangeException(nameof(this.ApiKey), "The API Key is empty");
        }
    }
}
