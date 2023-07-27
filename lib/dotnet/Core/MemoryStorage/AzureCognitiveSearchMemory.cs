// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.MemoryStorage;

public class AzureCognitiveSearchMemory
{
    public AzureCognitiveSearchMemory(string endpoint, string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("Azure Cognitive Search API key is empty");
        }

        AzureKeyCredential credentials = new(apiKey);
        this._adminClient = new SearchIndexClient(new Uri(endpoint), credentials, GetClientOptions());
    }

    #region private

    /// <summary>
    /// Index names cannot contain special chars. We use this rule to replace a few common ones
    /// with an underscore and reduce the chance of errors. If other special chars are used, we leave it
    /// to the service to throw an error.
    /// Note:
    /// - replacing chars introduces a small chance of conflicts, e.g. "the-user" and "the_user".
    /// - we should consider whether making this optional and leave it to the developer to handle.
    /// </summary>
    // private static readonly Regex s_replaceIndexNameSymbolsRegex = new(@"[\s|\\|/|.|_|:]");

    // private readonly ConcurrentDictionary<string, SearchClient> _clientsByIndex = new();
    private readonly SearchIndexClient _adminClient;

    /// <summary>
    /// Options used by the Azure Cognitive Search client, e.g. User Agent.
    /// See also https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/core/Azure.Core/src/DiagnosticsOptions.cs
    /// </summary>
    private static SearchClientOptions GetClientOptions()
    {
        return new SearchClientOptions
        {
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            },
        };
    }

    #endregion
}
