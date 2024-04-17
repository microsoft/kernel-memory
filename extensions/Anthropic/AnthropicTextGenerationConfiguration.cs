using System;

namespace SemanticMemory.Extensions.Anthropic;

/// <summary>
/// Configuration for Text Generation with Anthropic
/// </summary>
public class AnthropicTextGenerationConfiguration
{
    /// <summary>
    /// This allows configuring the client that will be used for httpclient factory to create client.
    /// It can be left empty if the default client is to be used.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// This allows configuring the maximum token total that can be generated. Default is 2048.
    /// </summary>
    public int MaxTokenTotal { get; set; } = 2048;

    /// <summary>
    /// Api key needed to access the Anthropic API
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the model to use for text generation
    /// </summary>
    public string ModelName { get; set; } = HaikuModelName;

    /// <summary>
    /// Haiku
    /// </summary>
    public const string HaikuModelName = "claude-3-haiku-20240307";

    /// <summary>
    /// Sonnet
    /// </summary>
    public const string SonnetModelName = "claude-3-sonnet-20240229";

    /// <summary>
    /// Opus
    /// </summary>
    public const string OpusModelName = "claude-3-opus-20240229";

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
