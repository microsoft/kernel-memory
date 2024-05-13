// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.FileSystem.DevTools;

#pragma warning disable CA1031 // need to catch all exceptions

/// <summary>
/// Simple file system abstraction that saves data to text files.
/// </summary>
internal sealed class DiskFileSystem : IFileSystem
{
    private const string DefaultVolumeName = "__default__";
    private static readonly Regex s_invalidCharsRegex = new(@"[\s|\||\\|/|\0|'|\`|""|:|;|,|~|!|?|*|+|=|^|@|#|$|%|&]");

    private readonly ILogger _log;
    private readonly IMimeTypeDetection _mimeTypeDetection;
    private readonly string _dataPath;

    public DiskFileSystem(
        string directory,
        IMimeTypeDetection? mimeTypeDetection = null,
        ILogger? log = null)
    {
        this._dataPath = directory;
        this._mimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
        this._log = log ?? DefaultLogger<DiskFileSystem>.Instance;
        this.CreateDirectory(this._dataPath);
    }

    #region Volume API

    /// <inheritdoc />
    public Task CreateVolumeAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        var path = Path.Join(this._dataPath, volume);
#pragma warning disable CA1849 // there is no async option in .NET 6
        this.CreateDirectory(path);
#pragma warning restore CA1849
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> VolumeExistsAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        var path = Path.Join(this._dataPath, volume);
        return Task.FromResult(Directory.Exists(path));
    }

    /// <inheritdoc />
    public async Task DeleteVolumeAsync(string volume, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        var path = Path.Join(this._dataPath, volume);
        this._log.LogWarning("Deleting directory: {0}", path);
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (IOException e) when (e.Message.Contains("not empty", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(attempt * 75), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> ListVolumesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<string>();
        if (Directory.Exists(this._dataPath))
        {
            var list = Directory.GetDirectories(this._dataPath);
            result.AddRange(list.Select(Path.GetFileName)!);
        }

        return Task.FromResult((IEnumerable<string>)result);
    }

    #endregion

    #region Directory API

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        this.CreateDirectory(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region File API

    /// <inheritdoc />
    public async Task WriteFileAsync(string volume, string relPath, string fileName, Stream streamContent, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        var path = Path.Join(this._dataPath, volume);
#pragma warning disable CA1849 // there is no async option in .NET 6
        this.CreateDirectory(path);
#pragma warning restore CA1849
        relPath = ValidatePath(relPath);
        fileName = ValidateFileName(fileName);
        path = Path.Join(path, relPath, fileName);
        this._log.LogTrace("Writing file to {0}", path);
        BinaryData data = await BinaryData.FromStreamAsync(streamContent, cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(path, data.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(string volume, string relPath, string fileName, string data, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        var path = Path.Join(this._dataPath, volume);
#pragma warning disable CA1849 // there is no async option in .NET 6
        this.CreateDirectory(path);
#pragma warning restore CA1849
        relPath = ValidatePath(relPath);
        fileName = ValidateFileName(fileName);
        path = Path.Join(path, relPath, fileName);
        this._log.LogTrace("Writing file to {0}", path);
        await File.WriteAllBytesAsync(path, new BinaryData(data).ToArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BinaryData> ReadFileAsBinaryAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        fileName = ValidateFileName(fileName);
        path = Path.Join(path, fileName);
        if (!File.Exists(path))
        {
            this._log.LogError("File not found: {0}", path);
            throw new FileNotFoundException($"File not found: {path}");
        }

        this._log.LogTrace("File exists, reading {0}", path);
        byte[] content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        this._log.LogTrace("File {0} size: {1} bytes", path, content.Length);
        return new BinaryData(content);
    }

    /// <inheritdoc />
    public Task<StreamableFileContent> ReadFileInfoAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        fileName = ValidateFileName(fileName);
        path = Path.Join(path, fileName);
        if (!File.Exists(path))
        {
            this._log.LogError("File not found: {0}", path);
            throw new FileNotFoundException($"File not found: {path}");
        }

        this._log.LogTrace("File exists, reading {0}", path);
        FileInfo info = new(path);
        var fileType = this._mimeTypeDetection.GetFileType(fileName);
        Task<Stream> AsyncStreamDelegate() => Task.FromResult<Stream>(info.OpenRead());
        StreamableFileContent result = new(fileName, info.Length, fileType, info.LastWriteTimeUtc, AsyncStreamDelegate);

        this._log.LogTrace("File {0} size: {1} bytes", path, info.Length);
        return Task.FromResult<StreamableFileContent>(result);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsTextAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        return (await this.ReadFileAsBinaryAsync(volume: volume, relPath: relPath, fileName: fileName, cancellationToken).ConfigureAwait(false))
            .ToString();
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAllFileNamesAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        var result = new List<string>();
        string[] fileEntries = Directory.GetFiles(path);
        foreach (string rawName in fileEntries)
        {
            var fileName = rawName;
            if (fileName.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                fileName = rawName.Substring(path.Length).Trim('/').Trim('\\');
            }

            // Note: the name doesn't include the path
            // Note: the list doesn't include files in sub dirs
            result.Add(fileName);
        }

        return Task.FromResult((IEnumerable<string>)result);
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath, fileName);
        return Task.FromResult(File.Exists(path));
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath, fileName);
        this._log.LogDebug("Deleting {0}", path);
        if (File.Exists(path)) { File.Delete(path); }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> ReadAllFilesAsTextAsync(string volume, string relPath, CancellationToken cancellationToken = default)
    {
        volume = ValidateVolumeName(volume);
        relPath = ValidatePath(relPath);
        var path = Path.Join(this._dataPath, volume, relPath);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        var result = new Dictionary<string, string>();
        string[] fileEntries = Directory.GetFiles(path);
        foreach (string fileName in fileEntries)
        {
            result[fileName] = new BinaryData(await File.ReadAllBytesAsync(fileName, cancellationToken).ConfigureAwait(false)).ToString();
        }

        return result;
    }

    #endregion

    #region private

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

    private void CreateDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || Directory.Exists(path))
        {
            return;
        }

        this._log.LogDebug("Creating directory {0}", path);
        Directory.CreateDirectory(path);
    }

    #endregion

#pragma warning restore CA1031
}
