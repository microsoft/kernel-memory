// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Serialization;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class AzureAIDocIntelConfig
{
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
    }

    /// <summary>
    /// Azure authentication type
    /// </summary>
    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;

    /// <summary>
    /// Optional custom auth tokens audience for sovereign clouds, when using Auth.AzureIdentity
    /// See https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/formrecognizer/Azure.AI.FormRecognizer/src/DocumentAnalysisAudience.cs
    /// Examples:
    /// - "https://cognitiveservices.azure.com"
    /// - "https://cognitiveservices.azure.us"
    /// - "https://cognitiveservices.azure.cn"
    /// </summary>
    public string? AzureIdentityAudience { get; set; } = null;

    /// <summary>
    /// API key, required if Auth == APIKey
    /// </summary>
    public string APIKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Document Intelligence resource endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        if (this.Auth == AuthTypes.APIKey && string.IsNullOrWhiteSpace(this.APIKey))
        {
            throw new ConfigurationException($"Azure AI Document Intelligence: {nameof(this.APIKey)} is empty");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ConfigurationException($"Azure AI Document Intelligence: {nameof(this.Endpoint)} is empty");
        }

        if (!this.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException($"Azure AI Document Intelligence: {nameof(this.Endpoint)} must start with https://");
        }
    }
}
