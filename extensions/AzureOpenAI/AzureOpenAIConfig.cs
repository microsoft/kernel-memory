// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Core;
using Microsoft.KernelMemory.Configuration;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

#pragma warning disable CA1024 // properties would need to require serializer cfg to ignore them
/// <summary>
/// Azure OpenAI settings.
/// </summary>
public class AzureOpenAIConfig
{
    private TokenCredential? _tokenCredential;

    public enum AuthTypes
    {
        Unknown = -1,
        AzureIdentity,
        APIKey,
        ManualTokenCredential,
    }

    public enum APITypes
    {
        Unknown = -1,
        TextCompletion,
        ChatCompletion,
        ImageGeneration,
        EmbeddingGeneration,
    }

    /// <summary>
    /// OpenAI API type, e.g. text completion, chat completion, image generation, etc.
    /// </summary>
    public APITypes APIType { get; set; } = APITypes.ChatCompletion;

    /// <summary>
    /// Azure authentication type
    /// </summary>
    public AuthTypes Auth { get; set; }

    /// <summary>
    /// Azure OpenAI endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI deployment name
    /// </summary>
    public string Deployment { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by model deployed.
    /// </summary>
    public int MaxTokenTotal { get; set; } = 8191;

    /// <summary>
    /// API key, required if Auth == APIKey
    /// </summary>
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// How many times to retry in case of throttling.
    /// </summary>
    public int MaxRetries { get; set; } = 10;

    /// <summary>
    /// Set credentials manually from code
    /// </summary>
    /// <param name="credential">Token credentials</param>
    public void SetCredential(TokenCredential credential)
    {
        this.Auth = AuthTypes.ManualTokenCredential;
        this._tokenCredential = credential;
    }

    /// <summary>
    /// Fetch the credentials passed manually from code.
    /// </summary>
    public TokenCredential GetTokenCredential()
    {
        return this._tokenCredential
               ?? throw new ConfigurationException("TokenCredential not defined");
    }

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.Auth == AuthTypes.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(this.Auth), "The authentication type is not defined");
        }

        if (this.Auth == AuthTypes.APIKey && string.IsNullOrWhiteSpace(this.APIKey))
        {
            throw new ArgumentOutOfRangeException(nameof(this.APIKey), "The API Key is empty");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ArgumentOutOfRangeException(nameof(this.Endpoint), "The endpoint value is empty");
        }

        if (!this.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(this.Endpoint), "The endpoint value must start with https://");
        }

        if (string.IsNullOrWhiteSpace(this.Deployment))
        {
            throw new ArgumentOutOfRangeException(nameof(this.Deployment), "The deployment value is empty");
        }

        if (this.MaxTokenTotal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MaxTokenTotal),
                $"{nameof(this.MaxTokenTotal)} cannot be less than 1");
        }
    }
}
