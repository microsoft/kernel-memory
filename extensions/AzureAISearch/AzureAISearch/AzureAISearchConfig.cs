// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure.Core;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

#pragma warning disable CA1024 // properties would need to require serializer cfg to ignore them
public class AzureAISearchConfig
{
    private TokenCredential? _tokenCredential;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthTypes
    {
        Unknown = -1,
        AzureIdentity,
        APIKey,
        ManualTokenCredential,
    }

    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;
    public string Endpoint { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Important: when using hybrid search, relevance scores
    /// are very different (e.g. lower) from when using just vector search.
    /// </summary>
    public bool UseHybridSearch { get; set; } = false;

    public void SetCredential(TokenCredential credential)
    {
        this.Auth = AuthTypes.ManualTokenCredential;
        this._tokenCredential = credential;
    }

    public TokenCredential GetTokenCredential()
    {
        return this._tokenCredential
               ?? throw new ConfigurationException($"Azure AI Search: {nameof(this._tokenCredential)} not defined");
    }
}
