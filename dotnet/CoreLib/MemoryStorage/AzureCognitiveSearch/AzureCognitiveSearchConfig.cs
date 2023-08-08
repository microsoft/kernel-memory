// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure.Core;
using Microsoft.SemanticMemory.Core.Configuration;

namespace Microsoft.SemanticMemory.Core.MemoryStorage.AzureCognitiveSearch;

#pragma warning disable CA1024 // properties would need to require serializer cfg to ignore them
public class AzureCognitiveSearchConfig
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
    public string VectorIndexPrefix { get; set; } = string.Empty;

    public void SetCredential(TokenCredential credential)
    {
        this.Auth = AuthTypes.ManualTokenCredential;
        this._tokenCredential = credential;
    }

    public TokenCredential GetTokenCredential()
    {
        return this._tokenCredential
               ?? throw new ConfigurationException("TokenCredential not defined");
    }
}
