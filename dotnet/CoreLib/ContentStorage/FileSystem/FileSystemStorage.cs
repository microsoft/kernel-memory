// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.ContentStorage.FileSystem;

public class FileSystemStorage : IContentStorage
{
    // Parent directory of the directory containing messages
    private readonly string _directory;

    // Application logger
    private readonly ILogger<FileSystemStorage> _log;

    public FileSystemStorage(FileSystemConfig config, ILogger<FileSystemStorage>? log = null)
    {
        this._log = log ?? DefaultLogger<FileSystemStorage>.Instance;
        this.CreateDirectory(config.Directory);
        this._directory = config.Directory;
    }

    /// <inherit />
    public string JoinPaths(string path1, string path2)
    {
        return Path.Join(path1, path2);
    }

    /// <inherit />
    public Task CreateDirectoryAsync(string directoryName, CancellationToken cancellationToken = default)
    {
        var path = Path.Join(this._directory, directoryName);

        if (!Directory.Exists(path))
        {
            this._log.LogDebug("Creating directory {0}", path);
            Directory.CreateDirectory(path);
        }

        return Task.CompletedTask;
    }

    /// <inherit />
    public async Task WriteTextFileAsync(string directoryName, string fileName, string fileContent, CancellationToken cancellationToken = default)
    {
        await this.CreateDirectoryAsync(directoryName, cancellationToken).ConfigureAwait(false);
        var path = Path.Join(this._directory, directoryName, fileName);
        this._log.LogDebug("Writing file {0}", path);
        await File.WriteAllTextAsync(path, fileContent, cancellationToken).ConfigureAwait(false);
    }

    /// <inherit />
    public async Task<long> WriteStreamAsync(string directoryName, string fileName, Stream contentStream, CancellationToken cancellationToken = default)
    {
        await this.CreateDirectoryAsync(directoryName, cancellationToken).ConfigureAwait(false);
        var path = Path.Join(this._directory, directoryName, fileName);

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
    public Task<BinaryData> ReadFileAsync(string directoryName, string fileName, bool errIfNotFound = true, CancellationToken cancellationToken = default)
    {
        var path = Path.Join(this._directory, directoryName, fileName);
        if (!File.Exists(path))
        {
            if (errIfNotFound) { this._log.LogError("File not found {0}", path); }

            throw new ContentStorageFileNotFoundException("File not found");
        }

        byte[] data = File.ReadAllBytes(path);
        return Task.FromResult(new BinaryData(data));
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
}
