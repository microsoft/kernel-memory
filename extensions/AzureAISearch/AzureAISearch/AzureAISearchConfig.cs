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

        // AzureIdentity: use automatic Entra (AAD) authentication mechanism.
        //   When the service is on sovereign clouds you can use the AZURE_AUTHORITY_HOST env var to
        //   set the authority host. See https://learn.microsoft.com/dotnet/api/overview/azure/identity-readme
        //   You can test locally using the AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET env vars.
        AzureIdentity,

        APIKey,
        ManualTokenCredential,
    }

    /// <summary>
    /// Azure authentication type
    /// </summary>
    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;

    /// <summary>
    /// Optional custom auth tokens audience for sovereign clouds, when using Auth.AzureIdentity
    /// See https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/search/Azure.Search.Documents/src/SearchAudience.cs
    /// Examples:
    /// - "https://search.azure.com"
    /// - "https://search.azure.us"
    /// - "https://search.azure.cn"
    /// </summary>
    public string? AzureIdentityAudience { get; set; } = null;

    /// <summary>
    /// API key, required if Auth == APIKey
    /// </summary>
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Search resource endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Important: when using hybrid search, relevance scores
    /// are very different (e.g. lower) from when using just vector search.
    /// </summary>
    public bool UseHybridSearch { get; set; } = false;

    /// <summary>
    /// Helps improve relevance score consistency for search services with multiple replicas by
    /// attempting to route a given request to the same replica for that session. Use this when
    /// favoring consistent scoring over lower latency. Can adversely affect performance.
    ///
    /// Whether to use sticky sessions, which can help getting more consistent results.
    /// When using sticky sessions, a best-effort attempt will be made to target the same replica set.
    /// Be wary that reusing the same replica repeatedly can interfere with the load balancing of
    /// the requests across replicas and adversely affect the performance of the search service.
    ///
    /// See https://learn.microsoft.com/rest/api/searchservice/documents/search-post?view=rest-searchservice-2024-07-01&amp;tabs=HTTP#request-body
    /// </summary>
    public bool UseStickySessions { get; set; } = false;

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
