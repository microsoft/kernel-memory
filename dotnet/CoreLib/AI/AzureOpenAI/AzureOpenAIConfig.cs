// Copyright (c) Microsoft. All rights reserved.

using Azure.Core;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.AI.AzureOpenAI;

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
}
