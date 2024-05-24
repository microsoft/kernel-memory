// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.DocumentStorage;

public interface IDocumentStorage
{
    /// <summary>
    /// Create a new container (aka index), if it doesn't exist already
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task CreateIndexDirectoryAsync(
        string index,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a container (aka index)
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task DeleteIndexDirectoryAsync(
        string index,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new container, if it doesn't exist already
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task CreateDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all artifacts of a document, except for the status file
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task EmptyDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all artifacts of a document, including status file and the containing folder
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create/Overwrite a file
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="streamContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteFileAsync(
        string index,
        string documentId,
        string fileName,
        Stream streamContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from storage
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName"></param>
    /// <param name="logErrIfNotFound">Whether to log an error if the file does not exist. An exception will be raised anyway.</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>File content</returns>
    Task<StreamableFileContent> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool logErrIfNotFound = true,
        CancellationToken cancellationToken = default);
}
