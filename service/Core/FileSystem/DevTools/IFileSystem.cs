// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.FileSystem.DevTools;

internal interface IFileSystem
{
    #region Volume API

    Task CreateVolumeAsync(string volume, CancellationToken cancellationToken = default);
    Task<bool> VolumeExistsAsync(string volume, CancellationToken cancellationToken = default);
    Task DeleteVolumeAsync(string volume, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListVolumesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Directory API

    Task CreateDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default);
    Task DeleteDirectoryAsync(string volume, string relPath, CancellationToken cancellationToken = default);

    #endregion

    #region File API

    Task WriteFileAsync(string volume, string relPath, string fileName, Stream streamContent, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string volume, string relPath, string fileName, string data, CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default);

    Task<BinaryData> ReadFileAsBinaryAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default);
    Task<StreamableFileContent> ReadFileInfoAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default);
    Task<string> ReadFileAsTextAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default);
    Task<IDictionary<string, string>> ReadAllFilesAsTextAsync(string volume, string relPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllFileNamesAsync(string volume, string relPath, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string volume, string relPath, string fileName, CancellationToken cancellationToken = default);

    #endregion
}
