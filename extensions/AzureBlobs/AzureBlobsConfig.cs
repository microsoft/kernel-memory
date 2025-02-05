// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure;
using Azure.Core;
using Azure.Storage;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

#pragma warning disable CA1024 // properties would need to require serializer cfg to ignore them
public class AzureBlobsConfig
{
    private StorageSharedKeyCredential? _storageSharedKeyCredential;
    private AzureSasCredential? _azureSasCredential;
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

        ConnectionString,
        AccountKey,
        ManualStorageSharedKeyCredential,
        ManualAzureSasCredential,
        ManualTokenCredential,
    }

    /// <summary>
    /// Azure authentication type
    /// </summary>
    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;

    /// <summary>
    /// Optional custom auth tokens audience when using Auth.AzureIdentity
    /// Example: "https://storage.azure.com/"
    /// </summary>
    public string? AzureIdentityAudience { get; set; } = null;

    public string ConnectionString { get; set; } = "";
    public string Account { get; set; } = "";
    public string AccountKey { get; set; } = "";
    public string EndpointSuffix { get; set; } = "core.windows.net";
    public string Container { get; set; } = "";

    public void SetCredential(StorageSharedKeyCredential credential)
    {
        this.Auth = AuthTypes.ManualStorageSharedKeyCredential;
        this._storageSharedKeyCredential = credential;
    }

    public void SetCredential(AzureSasCredential credential)
    {
        this.Auth = AuthTypes.ManualAzureSasCredential;
        this._azureSasCredential = credential;
    }

    public void SetCredential(TokenCredential credential)
    {
        this.Auth = AuthTypes.ManualTokenCredential;
        this._tokenCredential = credential;
    }

    public StorageSharedKeyCredential GetStorageSharedKeyCredential()
    {
        return this._storageSharedKeyCredential
               ?? throw new ConfigurationException($"Azure Blobs: {nameof(this._storageSharedKeyCredential)} not defined");
    }

    public AzureSasCredential GetAzureSasCredential()
    {
        return this._azureSasCredential
               ?? throw new ConfigurationException($"Azure Blobs: {nameof(this._azureSasCredential)} not defined");
    }

    public TokenCredential GetTokenCredential()
    {
        return this._tokenCredential
               ?? throw new ConfigurationException($"Azure Blobs: {nameof(this._tokenCredential)} not defined");
    }
}
