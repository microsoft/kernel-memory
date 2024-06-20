// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DocumentStorage.AzureBlobs;

// TODO: a container can contain up to 50000 blocks
// TODO: optionally use one container per index
[Experimental("KMEXP03")]
public sealed class AzureBlobsStorage : IDocumentStorage
{
    private const string DefaultContainerName = "smemory";
    private const string DefaultEndpointSuffix = "core.windows.net";

    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobsStorage> _log;
    private readonly IMimeTypeDetection _mimeTypeDetection;

    public AzureBlobsStorage(
        AzureBlobsConfig config,
        IMimeTypeDetection? mimeTypeDetection = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._mimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<AzureBlobsStorage>();

        BlobServiceClient client;
        switch (config.Auth)
        {
            case AzureBlobsConfig.AuthTypes.ConnectionString:
            {
                this.ValidateConnectionString(config.ConnectionString);
                client = new BlobServiceClient(config.ConnectionString);
                break;
            }

            case AzureBlobsConfig.AuthTypes.AccountKey:
            {
                this.ValidateAccountName(config.Account);
                this.ValidateAccountKey(config.AccountKey);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                client = new BlobServiceClient(new Uri($"https://{config.Account}.blob.{suffix}"), new StorageSharedKeyCredential(config.Account, config.AccountKey));
                break;
            }

            case AzureBlobsConfig.AuthTypes.AzureIdentity:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                client = new BlobServiceClient(new Uri($"https://{config.Account}.blob.{suffix}"), new DefaultAzureCredential());
                break;
            }

            case AzureBlobsConfig.AuthTypes.ManualStorageSharedKeyCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                client = new BlobServiceClient(new Uri($"https://{config.Account}.blob.{suffix}"), config.GetStorageSharedKeyCredential());
                break;
            }

            case AzureBlobsConfig.AuthTypes.ManualAzureSasCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                client = new BlobServiceClient(new Uri($"https://{config.Account}.blob.{suffix}"), config.GetAzureSasCredential());
                break;
            }

            case AzureBlobsConfig.AuthTypes.ManualTokenCredential:
            {
                this.ValidateAccountName(config.Account);
                var suffix = this.ValidateEndpointSuffix(config.EndpointSuffix);
                client = new BlobServiceClient(new Uri($"https://{config.Account}.blob.{suffix}"), config.GetTokenCredential());
                break;
            }

            default:
                this._log.LogCritical("Azure Blob authentication type '{0}' undefined or not supported", config.Auth);
                throw new DocumentStorageException($"Azure Blob authentication type '{config.Auth}' undefined or not supported");
        }

        this._containerName = config.Container;
        if (string.IsNullOrEmpty(this._containerName))
        {
            this._containerName = DefaultContainerName;
            this._log.LogError("The Azure Blob container name is empty, using default value {0}", this._containerName);
        }

        this._containerClient = client.GetBlobContainerClient(this._containerName);
        if (this._containerClient == null)
        {
            this._log.LogCritical("Unable to instantiate Azure Blob container client");
            throw new DocumentStorageException("Unable to instantiate Azure Blob container client");
        }
    }

    /// <inheritdoc />
    public async Task CreateIndexDirectoryAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        // Note: Azure Blobs doesn't have an artifact for "directories", which are just a detail in a blob name
        //       so there's no such thing as creating a directory. When creating a blob, the name must contain
        //       the directory name, e.g. blob.Name = "dir1/subdir2/file.txt"

        this._log.LogTrace("Creating container '{0}' ...", this._containerName);

        await this._containerClient
            .CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        this._log.LogTrace("Container '{0}' ready", this._containerName);
    }

    /// <inheritdoc />
    public Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            throw new DocumentStorageException("The index name is empty, stopping the process to prevent data loss");
        }

        return this.DeleteBlobsByPrefixAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreateDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // Note: Azure Blobs doesn't have an artifact for "directories", which are just a detail in a blob name
        //       so there's no such thing as creating a directory. When creating a blob, the name must contain
        //       the directory name, e.g. blob.Name = "dir1/subdir2/file.txt"

        this._log.LogTrace("Creating container '{0}' ...", this._containerName);

        await this._containerClient
            .CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        this._log.LogTrace("Container '{0}' ready", this._containerName);
    }

    /// <inheritdoc />
    public Task EmptyDocumentDirectoryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(directoryName))
        {
            throw new DocumentStorageException("The index, or document ID, or directory name is empty, stopping the process to prevent data loss");
        }

        return this.DeleteBlobsByPrefixAsync(directoryName, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(directoryName))
        {
            throw new DocumentStorageException("The index, or document ID, or directory name is empty, stopping the process to prevent data loss");
        }

        return this.DeleteBlobsByPrefixAsync(directoryName, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteFileAsync(
        string index,
        string documentId,
        string fileName,
        Stream streamContent,
        CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        var fileType = this._mimeTypeDetection.GetFileType(fileName);
        return this.InternalWriteAsync(
            directoryName: directoryName,
            fileName: fileName,
            fileType: fileType,
            content: streamContent,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StreamableFileContent> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool logErrIfNotFound = true,
        CancellationToken cancellationToken = default)
    {
        // IMPORTANT: documentId can be empty, e.g. when deleting an index
        ArgumentNullExceptionEx.ThrowIfNullOrEmpty(index, nameof(index), "Index name is empty");
        ArgumentNullExceptionEx.ThrowIfNullOrEmpty(fileName, nameof(fileName), "Filename is empty");

        var directoryName = JoinPaths(index, documentId);
        var blobName = $"{directoryName}/{fileName}";
        BlobClient blobClient = this.GetBlobClient(blobName);

        try
        {
            bool exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                var properties = (await blobClient.GetPropertiesAsync(null, cancellationToken).ConfigureAwait(false)).Value;
                return new StreamableFileContent(
                    blobClient.Name,
                    properties.ContentLength,
                    properties.ContentType,
                    properties.LastModified,
                    async () => (await blobClient.DownloadStreamingAsync(null, cancellationToken).ConfigureAwait(false)).Value.Content);
            }

            if (logErrIfNotFound) { this._log.LogError("Unable to download file {0}", blobName); }

            throw new DocumentStorageFileNotFoundException("Unable to fetch blob content");
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            this._log.LogInformation("File not found: {0}", blobName);
            throw new DocumentStorageFileNotFoundException("File not found", e);
        }
    }

    #region private

    /// <summary>
    /// Join index name and document ID, using the platform specific logic, to calculate the directory name
    /// </summary>
    /// <param name="index">Index name, left side of the path</param>
    /// <param name="documentId">Document ID, right side of the path (appended to index)</param>
    /// <returns>Index name concatenated with Document Id into a single path</returns>
    private static string JoinPaths(string index, string documentId)
    {
        return $"{index}/{documentId}";
    }

    private async Task InternalWriteAsync(
        string directoryName,
        string fileName,
        string fileType,
        object content,
        CancellationToken cancellationToken)
    {
        var blobName = $"{directoryName}/{fileName}";

        BlobClient blobClient = this.GetBlobClient(blobName);

        BlobUploadOptions options = new();
        BlobLeaseClient? blobLeaseClient = null;
        BlobLease? lease = null;
        if (await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            blobLeaseClient = this.GetBlobLeaseClient(blobClient);
            lease = await this.LeaseBlobAsync(blobLeaseClient, cancellationToken).ConfigureAwait(false);
            options = new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = lease.LeaseId } };
        }

        options.HttpHeaders = new BlobHttpHeaders { ContentType = fileType };

        this._log.LogTrace("Writing blob {0} ...", blobName);

        long size;
        switch (content)
        {
            case string fileContent:
                await blobClient.UploadAsync(BinaryData.FromString(fileContent), options, cancellationToken).ConfigureAwait(false);
                size = fileContent.Length;
                break;
            case Stream stream:
                stream.Seek(0, SeekOrigin.Begin);
                await blobClient.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
                size = stream.Length;
                break;
            default:
                throw new DocumentStorageException($"Unexpected object type {content.GetType().FullName}");
        }

        if (size == 0)
        {
            this._log.LogWarning("The file {0}/{1} is empty", directoryName, fileName);
        }

        await this.ReleaseBlobAsync(blobLeaseClient, lease, cancellationToken).ConfigureAwait(false);

        this._log.LogTrace("Blob {0} ready, size {1}", blobName, size);
    }

    private async Task DeleteBlobsByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new DocumentStorageException("The blob prefix is empty, stopping the process to prevent data loss");
        }

        this._log.LogInformation("Deleting blobs at {0}", prefix);

        AsyncPageable<BlobItem>? blobList = this._containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
        await foreach (Page<BlobItem> page in blobList.AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (BlobItem blob in page.Values)
            {
                // Skip root and empty names
                if (string.IsNullOrWhiteSpace(blob.Name) || blob.Name == prefix) { continue; }

                // Remove the prefix, skip root and empty names
                var fileName = blob.Name.Trim('/').Substring(prefix.Trim('/').Length).Trim('/');
                if (string.IsNullOrWhiteSpace(fileName)) { continue; }

                // Don't delete the pipeline status file
                if (fileName == Constants.PipelineStatusFilename) { continue; }

                this._log.LogInformation("Deleting blob {0}", blob.Name);
                Response? response = await this.GetBlobClient(blob.Name).DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (response.Status < 300)
                {
                    this._log.LogDebug("Delete response: {0}", response.Status);
                }
                else
                {
                    this._log.LogWarning("Unexpected delete response: {0}", response.Status);
                }
            }
        }
    }

    private BlobClient GetBlobClient(string blobName)
    {
        BlobClient? blobClient = this._containerClient.GetBlobClient(blobName);
        if (blobClient == null)
        {
            throw new DocumentStorageException("Unable to instantiate Azure Blob blob client");
        }

        return blobClient;
    }

    private BlobLeaseClient GetBlobLeaseClient(BlobClient blobClient)
    {
        var blobLeaseClient = blobClient.GetBlobLeaseClient();
        if (blobLeaseClient == null)
        {
            throw new DocumentStorageException("Unable to instantiate Azure blob lease client");
        }

        return blobLeaseClient;
    }

    private async Task<BlobLease> LeaseBlobAsync(BlobLeaseClient blobLeaseClient, CancellationToken cancellationToken)
    {
        this._log.LogTrace("Leasing blob {0} ...", blobLeaseClient.Uri);

        Response<BlobLease> lease = await blobLeaseClient
            .AcquireAsync(TimeSpan.FromSeconds(30), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (lease == null || !lease.HasValue)
        {
            throw new DocumentStorageException("Unable to lease blob");
        }

        this._log.LogTrace("Blob {0} leased", blobLeaseClient.Uri);

        return lease.Value;
    }

    private async Task ReleaseBlobAsync(BlobLeaseClient? blobLeaseClient, BlobLease? lease, CancellationToken cancellationToken)
    {
        if (lease != null && blobLeaseClient != null)
        {
            this._log.LogTrace("Releasing blob {0} ...", blobLeaseClient.Uri);
            await blobLeaseClient
                .ReleaseAsync(new BlobRequestConditions { LeaseId = lease.LeaseId }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            this._log.LogTrace("Blob released {0} ...", blobLeaseClient.Uri);
        }
    }

    private void ValidateAccountName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Blob account name is empty");
            throw new DocumentStorageException("The account name is empty");
        }
    }

    private void ValidateAccountKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Blob account key is empty");
            throw new DocumentStorageException("The Azure Blob account key is empty");
        }
    }

    private void ValidateConnectionString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            this._log.LogCritical("The Azure Blob connection string is empty");
            throw new DocumentStorageException("The Azure Blob connection string is empty");
        }
    }

    private string ValidateEndpointSuffix(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            value = DefaultEndpointSuffix;
            this._log.LogError("The Azure Blob account endpoint suffix is empty, using default value {0}", value);
        }

        return value;
    }

    #endregion
}
