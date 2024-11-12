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
    /// See https://learn.microsoft.com/rest/api/searchservice/documents/search-post?view=rest-searchservice-2024-07-01&tabs=HTTP#request-body
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
