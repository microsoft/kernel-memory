// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.MemoryStorage.DevTools;

internal interface ISimpleStorage
{
    Task<string> ReadAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task WriteAsync(string collection, string id, string data, CancellationToken cancellationToken = default);
    Task DeleteAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> ReadAllAsync(string collection, CancellationToken cancellationToken = default);
}

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

    public async Task<string> ReadAsync(string collection, string id, CancellationToken cancellationToken = default)
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

    public async Task<Dictionary<string, string>> ReadAllAsync(string collection, CancellationToken cancellationToken = default)
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

    public async Task WriteAsync(string collection, string id, string data, CancellationToken cancellationToken = default)
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

    public Task DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
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
}

/// <summary>
/// Simple storage that saves data in memory.
/// </summary>
internal sealed class VolatileStorage : ISimpleStorage
{
    private readonly ILogger _log;

    private readonly Dictionary<string, Dictionary<string, string>> _data = new();

    public VolatileStorage(ILogger? log = null)
    {
        this._log = log ?? DefaultLogger<VolatileStorage>.Instance;
    }

    public Task<string> ReadAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        if (this._data.TryGetValue(collection, out var collectionData))
        {
            if (collectionData.TryGetValue(id, out var data))
            {
                return Task.FromResult(data);
            }

            this._log.LogError("Volatile storage read failed: id not found");
            return Task.FromResult(string.Empty);
        }

        this._log.LogError("Volatile storage read failed: collection not found");
        return Task.FromResult(string.Empty);
    }

    public Task WriteAsync(string collection, string id, string data, CancellationToken cancellationToken = default)
    {
        if (this._data.TryGetValue(collection, out var collectionData))
        {
            collectionData[id] = data;
        }
        else
        {
            this._data[collection] = new Dictionary<string, string> { { id, data } };
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        if (this._data.TryGetValue(collection, out var collectionData))
        {
            collectionData.Remove(id);

            if (collectionData.Count == 0)
            {
                this._data.Remove(collection);
            }
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> ReadAllAsync(string collection, CancellationToken cancellationToken = default)
    {
        if (this._data.TryGetValue(collection, out var collectionData))
        {
            return Task.FromResult(collectionData);
        }

        return Task.FromResult(new Dictionary<string, string>());
    }
}

#pragma warning restore CA1031
