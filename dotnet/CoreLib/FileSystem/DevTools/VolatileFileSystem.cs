// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KernelMemory.FileSystem.DevTools;

/// <summary>
/// Simple file system abstraction that saves text files in memory.
/// </summary>
internal sealed class VolatileFileSystem : IFileSystem
{
    private const string DefaultVolumeName = "__default__";
    private const char DirSeparator = '/';
    private static readonly Regex s_invalidCharsRegex = new(@"[\s|\||\\|/|\0|'|\`|""|:|;|,|~|!|?|*|+|=|^|@|#|$|%|&]");
    private static VolatileFileSystem? s_singleton;

    private readonly ILogger _log;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BinaryData>> _volumes = new();

    /// <summary>
    /// Ctor accessible to unit tests only.
    /// </summary>
    internal VolatileFileSystem(ILogger? log = null)
    {
        this._log = log ?? DefaultLogger<VolatileFileSystem>.Instance;
    }

    /// <summary>
    /// Note: the volatile FS should be used as a singleton, in order to share state (directories and files)
    /// across clients. E.g. the simple queue requires a shared instance to work properly.
    /// </summary>
    public static VolatileFileSystem GetInstance(ILogger? log = null)
    {
        return s_singleton ??= new VolatileFileSystem(log);
    }

    #region Volume API

    public Task CreateVolumeAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        if (!this._volumes.ContainsKey(volume))
        {
            this._volumes.TryAdd(volume, new ConcurrentDictionary<string, BinaryData>());
        }

        return Task.CompletedTask;
    }

    public Task<bool> VolumeExistsAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        return Task.FromResult(this._volumes.ContainsKey(volume));
    }

    public Task DeleteVolumeAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        this._volumes.TryRemove(volume, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region Directory API

    public async Task CreateDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        await this.CreateVolumeAsync(volume, cancellationToken).ConfigureAwait(false);
        relPath = ValidatePath(relPath);
        // Note: the value has an / at the end
        var path = JoinPaths(relPath, "");
        this._volumes[volume][path] = new(string.Empty);
    }

    public async Task DeleteDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        if (this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
        {
            var files = await this.GetAllFileNamesAsync(volume, relPath, cancellationToken).ConfigureAwait(false);
            foreach (var fileName in files)
            {
                var path = JoinPaths(relPath, fileName);
                volumeData.TryRemove(path, out _);
            }

            var dirPath = JoinPaths(relPath, "");
            volumeData.TryRemove(dirPath, out _);
        }
    }

    #endregion

    #region File API

    public async Task WriteFileAsync(string volume, string relPath, string fileName, Stream streamContent, CancellationToken cancellationToken = default)
    {
        await using (streamContent.ConfigureAwait(false))
        {
            var data = new BinaryData(streamContent.ReadAllBytes());
            volume = ValidateVolumeName(volume);
            await this.ValidateVolumeExistsAsync(volume, cancellationToken).ConfigureAwait(false);

            if (!this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
            {
                this.ThrowVolumeNotFound(volume);
                return;
            }

            relPath = ValidatePath(relPath);
            fileName = ValidateFileName(fileName);
            var path = JoinPaths(relPath, fileName);
            volumeData[path] = data;
        }
    }

    public async Task WriteFileAsync(string volume, string relPath, string fileName, string data, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        await this.ValidateVolumeExistsAsync(volume, cancellationToken).ConfigureAwait(false);

        if (!this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
        {
            this.ThrowVolumeNotFound(volume);
            return;
        }

        relPath = ValidatePath(relPath);
        fileName = ValidateFileName(fileName);
        var path = JoinPaths(relPath, fileName);
        volumeData[path] = new BinaryData(data);
    }

    public Task<BinaryData> ReadFileAsBinaryAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        BinaryData result = new(string.Empty);

        if (this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
        {
            relPath = ValidatePath(relPath);
            fileName = ValidateFileName(fileName);
            var dirPath = JoinPaths(relPath, "");
            var filePath = JoinPaths(relPath, fileName);
            if (!volumeData.Keys.Any(x => x.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DirectoryNotFoundException($"Directory not found: {dirPath}");
            }

            if (!volumeData.TryGetValue(filePath, out result!))
            {
                this._log.LogError("File not found: {0}", filePath);
                throw new FileNotFoundException($"File not found: {filePath}");
            }
        }
        else
        {
            this.ThrowVolumeNotFound(volume);
        }

        return Task.FromResult(result);
    }

    public async Task<string> ReadFileAsTextAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        return (await this.ReadFileAsBinaryAsync(volume: volume, relPath: relPath, fileName: fileName, cancellationToken).ConfigureAwait(false))
            .ToString();
    }

    public Task<IEnumerable<string>> GetAllFileNamesAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var result = new List<string>();

        if (this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
        {
            // the value has a "/" at the end to correctly check for prefix
            var path = JoinPaths(relPath, "");
            // find all files starting with the prefix, excluding dirs
            result.AddRange(from entry in volumeData
                            where entry.Key.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                                  && entry.Key != path
                                  && !entry.Key.Substring(path.Length).Contains('/', StringComparison.Ordinal)
                            select entry.Key.Substring(path.Length));
        }
        else
        {
            this.ThrowVolumeNotFound(volume);
        }

        return Task.FromResult((IEnumerable<string>)result);
    }

    public Task<bool> FileExistsAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = JoinPaths(relPath, fileName);
        try
        {
            return Task.FromResult(this._volumes.ContainsKey(volume) && this._volumes[volume].ContainsKey(path) && !path.EndsWith($"{DirSeparator}", StringComparison.Ordinal));
        }
        catch (KeyNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    public Task DeleteFileAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        if (this._volumes.TryGetValue(volume, out var volumeData))
        {
            var path = JoinPaths(relPath, fileName);
            volumeData.TryRemove(path, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IDictionary<string, string>> ReadAllFilesAsTextAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        IDictionary<string, string> result = new Dictionary<string, string>();

        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        if (this._volumes.TryGetValue(volume, out ConcurrentDictionary<string, BinaryData>? volumeData))
        {
            // add "/" at the end to correctly check for prefix
            var path = JoinPaths(relPath, "");
            foreach (KeyValuePair<string, BinaryData> entry in volumeData)
            {
                if (entry.Key.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.ToString()));
                }
            }
        }

        return Task.FromResult(result);
    }

    #endregion

    #region private

    internal ConcurrentDictionary<string, ConcurrentDictionary<string, BinaryData>> GetInternalState()
    {
        return this._volumes;
    }

    private Task ValidateVolumeExistsAsync(string volume, CancellationToken cancellationToken)
    {
        if (!this._volumes.ContainsKey(volume))
        {
            this.ThrowVolumeNotFound(volume);
        }

        return Task.CompletedTask;
    }

    private void ThrowVolumeNotFound(string volume)
    {
        // Don't log errors here, this can be expected, let the caller handle the exception
        throw new DirectoryNotFoundException($"Volume not found: {volume}");
    }

    private static string ValidateVolumeName(string volume)
    {
        if (string.IsNullOrEmpty(volume))
        {
            return DefaultVolumeName;
        }

        if (s_invalidCharsRegex.Match(volume).Success)
        {
            throw new ArgumentException("The volume name contains some invalid chars or empty spaces");
        }

        return volume;
    }

    private static string ValidatePath(string path)
    {
        // Check invalid chars one at a time for better error messages
        if (path.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("The path contains some invalid chars: backslash '\\' chars are not allowed");
        }

        if (path.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("The path contains some invalid chars: colon ':' chars are not allowed");
        }

        return path;
    }

    private static string ValidateFileName(string fileName)
    {
        // Check invalid chars one at a time for better error messages
        if (fileName.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException($"The file name {fileName} contains some invalid chars: slash '/' chars are not allowed");
        }

        if (fileName.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException($"The file name {fileName} contains some invalid chars: backslash '\\' chars are not allowed");
        }

        if (fileName.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException($"The file name {fileName} contains some invalid chars: colon ':' chars are not allowed");
        }

        return fileName;
    }

    private static string JoinPaths(string a, string b)
    {
        return $"{a.Trim('/').Trim('\\')}{DirSeparator}{b.Trim('/').Trim('\\')}";
    }

    #endregion
}
