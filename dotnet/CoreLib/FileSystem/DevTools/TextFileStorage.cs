﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.FileSystem.DevTools;

#pragma warning disable CA1031 // need to catch all exceptions

/// <summary>
/// Simple storage that saves data to text files.
/// </summary>
internal sealed class TextFileStorage : ISimpleStorage
{
    private readonly ILogger _log;
    private readonly string _dataPath;

    public TextFileStorage(string directory, ILogger? log = null)
    {
        this._log = log ?? DefaultLogger<TextFileStorage>.Instance;
        this._dataPath = directory;
        this.CreateDirectory(this._dataPath);
    }

    public async Task<string> ReadFileAsTextAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = this.BuildFilename(collection, id);
            this._log.LogTrace("Reading {0}", fileName);
            if (File.Exists(fileName))
            {
                return await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Text file storage read failed");
        }

        return string.Empty;
    }

    public async Task<Dictionary<string, string>> ReadAllFilesAtTextAsync(string collection, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();
        var collectionPath = this.BuildCollectionPath(collection);
        if (!Directory.Exists(collectionPath)) { return result; }

        string[] fileEntries = Directory.GetFiles(collectionPath);
        foreach (string fileName in fileEntries)
        {
            var id = DecodeId(fileName.Split(Path.DirectorySeparatorChar).Last());
            result[id] = await File.ReadAllTextAsync(fileName, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task WriteFileAsync(string collection, string id, string data, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = this.BuildFilename(collection, id);
            var collectionPath = this.BuildCollectionPath(collection);
            if (!Directory.Exists(collectionPath)) { this.CreateDirectory(collectionPath); }

            this._log.LogTrace("Writing to {0}", fileName);
            await File.WriteAllTextAsync(fileName, data, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Text file storage write failed");
        }
    }

    public Task DeleteFileAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = this.BuildFilename(collection, id);
            this._log.LogDebug("Deleting {0}", fileName);
            if (File.Exists(fileName)) { File.Delete(fileName); }
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Text file storage deletion failed");
        }

        return Task.CompletedTask;
    }

    public Task DeleteCollectionAsync(string collection, CancellationToken cancellationToken = default)
    {
        try
        {
            var collectionPath = this.BuildCollectionPath(collection);
            this._log.LogDebug("Deleting collection {0}", collectionPath);
            if (Directory.Exists(collectionPath)) { Directory.Delete(collectionPath, recursive: true); }
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Collection deletion failed");
        }

        return Task.CompletedTask;
    }

    private string BuildFilename(string collection, string id)
    {
        var collectionPath = this.BuildCollectionPath(collection);
        if (!Directory.Exists(collectionPath))
        {
            Directory.CreateDirectory(collectionPath);
        }

        var filename = EncodeId(id);

        return Path.Combine(collectionPath, filename);
    }

    private string BuildCollectionPath(string collection)
    {
        return Path.Combine(this._dataPath, collection);
    }

    private void CreateDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || Directory.Exists(path))
        {
            return;
        }

        this._log.LogDebug("Creating directory {0}", path);
        Directory.CreateDirectory(path);
    }

    private static string EncodeId(string realId)
    {
        var bytes = Encoding.UTF8.GetBytes(realId);
        return Convert.ToBase64String(bytes).Replace('=', '_');
    }

    private static string DecodeId(string encodedId)
    {
        var bytes = Convert.FromBase64String(encodedId.Replace('_', '='));
        return Encoding.UTF8.GetString(bytes);
    }
#pragma warning restore CA1031
}
