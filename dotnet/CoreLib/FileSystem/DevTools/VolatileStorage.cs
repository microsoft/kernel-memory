// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

namespace Microsoft.SemanticMemory.FileSystem.DevTools;

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

    public Task<string> ReadFileAsTextAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        var result = string.Empty;
        if (this._data.TryGetValue(collection, out Dictionary<string, string>? collectionData))
        {
            if (collectionData.TryGetValue(id, out string? data))
            {
                result = data;
            }
            else
            {
                this._log.LogError("ID not found");
            }
        }
        else
        {
            this._log.LogError("Collection not found");
        }

        return Task.FromResult(result);
    }

    public async Task<BinaryData> ReadFileAsBinaryAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        return ToBinaryData(await this.ReadFileAsTextAsync(collection, id, cancellationToken).ConfigureAwait(false));
    }

    public Task<IEnumerable<string>> GetAllFileNamesAsync(string collection, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> result = new List<string>();

        if (this._data.TryGetValue(collection, out Dictionary<string, string>? collectionData))
        {
            result = collectionData.Values;
        }

        return Task.FromResult(result);
    }

    public Task WriteFileAsync(string collection, string id, string data, CancellationToken cancellationToken = default)
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

    public Task WriteFileAsync(string collection, string id, Stream data, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistsAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CollectionExistsAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(string collection, string id, CancellationToken cancellationToken = default)
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

    public Task DeleteCollectionAsync(string collection, CancellationToken cancellationToken = default)
    {
        this._data.Remove(collection);

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> ReadAllFilesAtTextAsync(string collection, CancellationToken cancellationToken = default)
    {
        if (this._data.TryGetValue(collection, out var collectionData))
        {
            return Task.FromResult(collectionData);
        }

        return Task.FromResult(new Dictionary<string, string>());
    }

    private static BinaryData ToBinaryData(string? content)
    {
        byte[] data = string.IsNullOrEmpty(content) ? Array.Empty<byte>() : Encoding.Unicode.GetBytes(content);
        return new BinaryData(data);
    }
}
