// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.FileSystem.DevTools;

namespace Microsoft.KernelMemory.ContentStorage.DevTools;

public class SimpleFileStorage : IContentStorage
{
    private readonly ILogger<SimpleFileStorage> _log;
    private readonly IFileSystem _fileSystem;

    public SimpleFileStorage(SimpleFileStorageConfig config, ILogger<SimpleFileStorage>? log = null)
    {
        this._log = log ?? DefaultLogger<SimpleFileStorage>.Instance;
        switch (config.StorageType)
        {
            case FileSystemTypes.Disk:
                this._fileSystem = new DiskFileSystem(config.Directory, this._log);
                break;

            case FileSystemTypes.Volatile:
                this._fileSystem = VolatileFileSystem.GetInstance(config.Directory, this._log);
                break;

            default:
                throw new ArgumentException($"Unknown storage type {config.StorageType}");
        }
    }

    /// <inheritdoc />
    public Task CreateIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        return this._fileSystem.CreateVolumeAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        return this._fileSystem.DeleteVolumeAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public Task CreateDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return this._fileSystem.CreateDirectoryAsync(index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EmptyDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        var files = await this._fileSystem.GetAllFileNamesAsync(index, documentId, cancellationToken).ConfigureAwait(false);
        foreach (string fileName in files)
        {
            // Don't delete the pipeline status file
            if (fileName == Constants.PipelineStatusFilename) { continue; }

            await this._fileSystem.DeleteFileAsync(index, documentId, fileName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return this._fileSystem.DeleteDirectoryAsync(index, documentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(
        string index,
        string documentId,
        string fileName,
        Stream streamContent,
        CancellationToken cancellationToken = default)
    {
        await this._fileSystem.CreateDirectoryAsync(volume: index, relPath: documentId, cancellationToken).ConfigureAwait(false);
        await this._fileSystem.WriteFileAsync(volume: index, relPath: documentId, fileName: fileName, streamContent: streamContent, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BinaryData> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool logErrIfNotFound = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await this._fileSystem.ReadFileAsBinaryAsync(volume: index, relPath: documentId, fileName: fileName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
        {
            if (logErrIfNotFound)
            {
                this._log.LogError("File not found {0}/{1}/{2}", index, documentId, fileName);
            }

            throw new ContentStorageFileNotFoundException("File not found");
        }
    }
}
