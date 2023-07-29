// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.Core.ContentStorage;

public interface IContentStorage
{
    /// <summary>
    /// Join two path/directory names using the platform specific char
    /// </summary>
    /// <param name="path1">Left side of the path</param>
    /// <param name="path2">Right side of the path (appended to path1)</param>
    /// <returns>Path1 concatenated with Path2 into a single path</returns>
    string JoinPaths(string path1, string path2);

    /// <summary>
    /// Create a new container, if it doesn't exist already
    /// </summary>
    /// <param name="directoryName">Name of the directory</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task CreateDirectoryAsync(string directoryName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create/Overwrite a file
    /// </summary>
    /// <param name="directoryName">Directory name (ie collection/directory)</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteTextFileAsync(string directoryName, string fileName, string fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create/Overwrite a file
    /// </summary>
    /// <param name="directoryName">Directory name (ie collection/directory)</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentStream">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task<long> WriteStreamAsync(string directoryName, string fileName, Stream contentStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from storage
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="fileName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BinaryData> ReadFileAsync(string directoryName, string fileName, CancellationToken cancellationToken = default);
}
