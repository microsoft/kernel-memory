// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Azure;
using Azure.Core;
using Azure.Storage;
using Microsoft.KernelMemory.Configuration;

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
        AzureIdentity,
        ConnectionString,
        AccountKey,
        ManualStorageSharedKeyCredential,
        ManualAzureSasCredential,
        ManualTokenCredential,
    }

    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;
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
               ?? throw new ConfigurationException("StorageSharedKeyCredential not defined");
    }

    public AzureSasCredential GetAzureSasCredential()
    {
        return this._azureSasCredential
               ?? throw new ConfigurationException("AzureSasCredential not defined");
    }

    public TokenCredential GetTokenCredential()
    {
        return this._tokenCredential
               ?? throw new ConfigurationException("TokenCredential not defined");
    }
}
