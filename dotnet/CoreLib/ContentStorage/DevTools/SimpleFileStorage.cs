// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.ContentStorage.DevTools;

public class SimpleFileStorage : IContentStorage
{
    // Parent directory of the directory containing messages
    private readonly string _directory;

    // Application logger
    private readonly ILogger<SimpleFileStorage> _log;

    public SimpleFileStorage(SimpleFileStorageConfig config, ILogger<SimpleFileStorage>? log = null)
    {
        this._log = log ?? DefaultLogger<SimpleFileStorage>.Instance;
        this.CreateDirectory(config.Directory);
        this._directory = config.Directory;
    }

    /// <inherit />
    public Task CreateIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        this.CreateDirectory(this.GetIndexPath(index));
        return Task.CompletedTask;
    }

    /// <inherit />
    public async Task CreateDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        await this.CreateIndexDirectoryAsync(index, cancellationToken).ConfigureAwait(false);
        this.CreateDirectory(this.GetDocumentPath(index, documentId));
    }

    /// <inherit />
    public async Task WriteTextFileAsync(
        string index,
        string documentId,
        string fileName,
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        await this.CreateDocumentDirectoryAsync(index, documentId, cancellationToken).ConfigureAwait(false);
        var path = this.GetFilePath(index, documentId, fileName);
        this._log.LogDebug("Writing file {0}", path);
        await File.WriteAllTextAsync(path, fileContent, cancellationToken).ConfigureAwait(false);
    }

    /// <inherit />
    public async Task<long> WriteStreamAsync(
        string index,
        string documentId,
        string fileName,
        Stream contentStream,
        CancellationToken cancellationToken = default)
    {
        await this.CreateDocumentDirectoryAsync(index, documentId, cancellationToken).ConfigureAwait(false);
        var path = this.GetFilePath(index, documentId, fileName);

        this._log.LogDebug("Creating file {0}", path);
        FileStream outputStream = File.Create(path);

        contentStream.Seek(0, SeekOrigin.Begin);

        this._log.LogDebug("Writing to file {0}", path);
        await contentStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        var size = outputStream.Length;
        outputStream.Close();
        return size;
    }

    /// <inherit />
    public Task<BinaryData> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool errIfNotFound = true,
        CancellationToken cancellationToken = default)
    {
        var path = this.GetFilePath(index, documentId, fileName);
        if (!File.Exists(path))
        {
            if (errIfNotFound) { this._log.LogError("File not found {0}", path); }

            throw new ContentStorageFileNotFoundException("File not found");
        }

        byte[] data = File.ReadAllBytes(path);
        return Task.FromResult(new BinaryData(data));
    }

    /// <inherit />
    public Task DeleteIndexDirectoryAsync(string index, CancellationToken cancellationToken = default)
    {
        this.DeleteDirectory(this.GetIndexPath(index));
        return Task.CompletedTask;
    }

    /// <inherit />
    public Task EmptyDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        string[] files = Directory.GetFiles(this.GetDocumentPath(index, documentId));
        foreach (string fileName in files)
        {
            // Don't delete the pipeline status file
            if (fileName == Constants.PipelineStatusFilename) { continue; }

            File.Delete(fileName);
        }

        return Task.CompletedTask;
    }

    /// <inherit />
    public Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        this.DeleteDirectory(this.GetDocumentPath(index, documentId));
        return Task.CompletedTask;
    }

    private string GetFilePath(string index, string documentId, string fileName)
    {
        return Path.Join(this.GetDocumentPath(index, documentId), fileName);
    }

    private string GetDocumentPath(string index, string documentId)
    {
        return Path.Join(this.GetIndexPath(index), documentId);
    }

    private string GetIndexPath(string index)
    {
        return Path.Join(this._directory, index);
    }

    private void CreateDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            this._log.LogDebug("Creating directory {0}", path);
            Directory.CreateDirectory(path);
        }
    }

    private void DeleteDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (Directory.Exists(path))
        {
            this._log.LogWarning("Deleting directory {0} ({1})", path, Path.GetFullPath(path));
            Directory.Delete(path, recursive: true);
        }
    }
}
