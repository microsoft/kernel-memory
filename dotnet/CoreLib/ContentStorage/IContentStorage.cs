// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.ContentStorage;

public interface IContentStorage
{
    /// <summary>
    /// Create a new container, if it doesn't exist already
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task CreateIndexDirectoryAsync(
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
    /// Create/Overwrite a file
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteTextFileAsync(
        string index,
        string documentId,
        string fileName,
        string fileContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create/Overwrite a file
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentStream">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task<long> WriteStreamAsync(
        string index,
        string documentId,
        string fileName,
        Stream contentStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from storage
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="fileName"></param>
    /// <param name="errIfNotFound">Whether to log an error if the file does not exist. An exception will be raised anyway.</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>File content</returns>
    Task<BinaryData> ReadFileAsync(
        string index,
        string documentId,
        string fileName,
        bool errIfNotFound = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all artifacts of a document
    /// </summary>
    /// <param name="index">Index name</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task DeleteDocumentDirectoryAsync(
        string index,
        string documentId,
        CancellationToken cancellationToken = default);
}
