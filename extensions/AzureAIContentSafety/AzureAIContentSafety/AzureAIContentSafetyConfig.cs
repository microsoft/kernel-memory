// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Azure.Core;

namespace Microsoft.KernelMemory.Safety.AzureAIContentSafety;

public class AzureAIContentSafetyConfig
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

    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;
    public string Endpoint { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public double GlobalSafetyThreshold { get; set; } = 0.0;
    public List<string> IgnoredWords { get; set; } = [];

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

    public void Validate()
    {
        if (this.Auth == AuthTypes.Unknown)
        {
            throw new ConfigurationException($"Azure AI Content Safety: {nameof(this.Auth)} (authentication type) is not defined");
        }

        if (this.Auth == AuthTypes.APIKey && string.IsNullOrWhiteSpace(this.APIKey))
        {
            throw new ConfigurationException($"Azure AI Content Safety: {nameof(this.APIKey)} is empty");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ConfigurationException($"Azure AI Content Safety: {nameof(this.Endpoint)} is empty");
        }

        if (!this.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException($"Azure AI Content Safety: {nameof(this.Endpoint)} must start with https://");
        }
    }
}
