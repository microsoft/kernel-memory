// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.DocumentStorage.S3;

public class S3Storage : IDocumentStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly ILogger<S3Storage> _log;

    public S3Storage(
        S3Config config,
        ILogger<S3Storage>? log = null)
    {
        this._log = log ?? DefaultLogger<S3Storage>.Instance;

        switch (config.Auth)
        {
            case S3Config.AuthTypes.AccessKey:
                {
                    this.ValidateAccessKey(config.AccessKey);
                    this.ValidateSecretKey(config.SecretKey);

                    this._client = new AmazonS3Client(
                        awsAccessKeyId: config.AccessKey,
                        awsSecretAccessKey: config.SecretKey,

                        clientConfig: new AmazonS3Config
                        {
                            ServiceURL = config.CustomHost + "/" + config.BucketName,
                            LogResponse = true
                        }
                    );
                    break;
                }

            default:
                this._log.LogCritical("Authentication type '{0}' undefined or not supported", config.Auth);
                throw new DocumentStorageException($"Authentication type '{config.Auth}' undefined or not supported");
        }

        if (string.IsNullOrEmpty(config.BucketName))
        {
            var msg = $"Bucket name '{config.BucketName}' undefined or not supported";
            this._log.LogCritical(msg);
            throw new DocumentStorageException(msg);
        }

        this._bucketName = config.BucketName;
    }

    /// <inheritdoc />
    public async Task CreateIndexDirectoryAsync(
        string index,
        CancellationToken cancellationToken = default)
    {
        // Note: AWS S3 doesn't have an artifact for "directories", which are just a detail
        //       in a object name so there's no such thing as creating a directory.
        //       For example, if you want to create a directory called "images" within a bucket,
        //       you can set the object key as "images/object.jpg". This would give the appearance
        //       of a file being stored in the "images" directory.

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(index))
        {
            throw new DocumentStorageException("The index name is empty, stopping the process to prevent data loss");
        }

        await this.DeleteObjectsByPrefixAsync(index, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CreateDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        // Note: AWS S3 doesn't have an artifact for "directories", which are just a detail in a blob name
        //       so there's no such thing as creating a directory. When creating a blob, the name must contain
        //       the directory name, e.g. blob.Name = "dir1/subdir2/file.txt"
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EmptyDocumentDirectoryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(directoryName))
        {
            throw new DocumentStorageException("The index, or document ID, or directory name is empty, stopping the process to prevent data loss");
        }

        await this.DeleteObjectsByPrefixAsync(directoryName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(directoryName))
        {
            throw new DocumentStorageException("The index, or document ID, or directory name is empty, stopping the process to prevent data loss");
        }

        await this.DeleteObjectsByPrefixAsync(directoryName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(
        string index,
        string documentId,
        string fileName,
        Stream streamContent,
        CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        var objName = $"{directoryName}/{fileName}";
        var len = streamContent.Length;

        this._log.LogTrace("Writing object {0} ...", objName);

        if (streamContent.Length == 0)
        {
            this._log.LogWarning("The file {0} is empty", objName);
        }

        await this._client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = this._bucketName,
            Key = objName,
            InputStream = streamContent
        }, cancellationToken: cancellationToken).ConfigureAwait(false);

        this._log.LogTrace("Object {0} ready, size {1}", objName, len);
    }

    /// <inheritdoc />
    public async Task<StreamableFileContent> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool logErrIfNotFound = true,
        CancellationToken cancellationToken = default)
    {
        var directoryName = JoinPaths(index, documentId);
        var objName = $"{directoryName}/{fileName}";

        try
        {
            using (var response = await this._client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = this._bucketName,
                    Key = objName,
                },
                cancellationToken: cancellationToken
            ).ConfigureAwait(false))
            {
                if (response == null)
                {
                    return new StreamableFileContent(
                        fileName,
                        0,
                        "application/octet-stream",
                        DateTimeOffset.UtcNow
                    );
                }

                using (var responseStream = response.ResponseStream)
                {
                    var memoryStream = new MemoryStream();

                    await responseStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

                    return new StreamableFileContent(
                        fileName,
                        response.ContentLength,
                        response.Headers.ContentType,
                        response.LastModified,
                        async () =>
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return await Task.FromResult(memoryStream).ConfigureAwait(false);
                        }
                    );
                }
            }
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            if (logErrIfNotFound)
            {
                this._log.LogInformation("File not found: {0}", objName);
            }

            throw new DocumentStorageFileNotFoundException("File not found", e);
        }
    }

    /// <summary>
    /// Generates a pre-signed URL for accessing an object stored in S3 for a limited time.
    /// </summary>
    /// <param name="index">The index directory in which the object resides.</param>
    /// <param name="documentId">The document directory under the index where the object resides.</param>
    /// <param name="fileName">The name of the file/object for which to generate the URL.</param>
    /// <param name="validDuration">The duration for which the URL should remain valid.</param>
    /// <returns>A pre-signed URL for accessing the object.</returns>
    public async Task<string> GeneratePreSignedURLAsync(string index, string documentId, string fileName, TimeSpan validDuration)
    {
        var directoryName = JoinPaths(index, documentId);
        var objectKey = $"{directoryName}/{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = this._bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(validDuration),
            Verb = HttpVerb.GET
        };

        try
        {
            var url = await this._client.GetPreSignedURLAsync(request).ConfigureAwait(false);
            this._log.LogTrace("Generated pre-signed URL for object {0}", objectKey);
            return url;
        }
        catch (Exception ex)
        {
            this._log.LogError("Error generating pre-signed URL for object {0}: {1}", objectKey, ex.Message);
            throw;
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

    private async Task DeleteObjectsByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new DocumentStorageException("The object prefix is empty, stopping the process to prevent data loss");
        }

        this._log.LogInformation("Deleting objects at {0}", prefix);

        var allObjects = new List<S3Object>();
        var request = new ListObjectsV2Request
        {
            BucketName = this._bucketName,
            Prefix = prefix
        };

        do
        {
            var response = await this._client.ListObjectsV2Async(
                request,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            allObjects.AddRange(response.S3Objects);

            if (!response.IsTruncated)
            {
                // Exit the loop if there are no more objects to retrieve
                break;
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (true);

        foreach (var obj in allObjects)
        {
            var fileName = obj.Key.Trim('/').Substring(prefix.Trim('/').Length).Trim('/');

            // Don't delete the pipeline status file
            if (fileName == Constants.PipelineStatusFilename) { continue; }

            this._log.LogInformation("Deleting blob {0}", obj.Key);

            var response = await this._client.DeleteObjectAsync(
                bucketName: this._bucketName,
                key: obj.Key,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            // 204 No Content: This status code indicates that the object was successfully deleted
            // from the bucket. The request was processed successfully, and there is no content
            // to return in the response.
            if (response.HttpStatusCode == HttpStatusCode.NoContent)
            {
                this._log.LogDebug("Delete response: {0}", response.HttpStatusCode);
            }
            else
            {
                this._log.LogWarning("Unexpected delete response: {0}", response.HttpStatusCode);
            }
        }
    }

    private void ValidateAccessKey(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            var msg = "The S3 Access Key is empty";
            this._log.LogCritical(msg);
            throw new DocumentStorageException(msg);
        }
    }

    private void ValidateSecretKey(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            var msg = "The S3 Secret Key is empty";
            this._log.LogCritical(msg);
            throw new DocumentStorageException(msg);
        }
    }

    #endregion
}
