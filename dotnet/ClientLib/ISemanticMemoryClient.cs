// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.Client.Models;

namespace Microsoft.SemanticMemory.Client;

public interface ISemanticMemoryClient
{
    /// <summary>
    /// Import a file into memory. The file can have tags and other details.
    /// </summary>
    /// <param name="file">Details of the file to import</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(Document file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import multiple files into memory. Each file can have tags and other details.
    /// </summary>
    /// <param name="files">Details of the files to import</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>List of document IDs</returns>
    public Task<IList<string>> ImportFilesAsync(Document[] files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import a file from disk into the default user memory.
    /// </summary>
    /// <param name="fileName">Path and name of the file to import</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import a files from disk into memory, with details such as tags and user ID.
    /// </summary>
    /// <param name="fileName">Path and name of the files to import</param>
    /// <param name="details">File details such as tags and user ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(string fileName, DocumentDetails details, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search the default user memory for an answer to the given query.
    /// </summary>
    /// <param name="query">Query/question to answer</param>
    /// <param name="filter">Filter to match</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Answer to the query, if possible</returns>
    public Task<MemoryAnswer> AskAsync(string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search a user memory for an answer to the given query.
    /// </summary>
    /// <param name="userId">ID of the user's memory to search</param>
    /// <param name="query">Query/question to answer</param>
    /// <param name="filter">Filter to match</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Answer to the query, if possible</returns>
    public Task<MemoryAnswer> AskAsync(string userId, string query, MemoryFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a document ID exists in a user memory and is ready for usage.
    /// The logic checks if the uploaded document has been fully processed.
    /// When the document exists in storage but is not processed yet, the method returns False.
    /// </summary>
    /// <param name="userId">ID of the user's memory to search</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>True if the document has been successfully uploaded and imported</returns>
    public Task<bool> IsReadyAsync(string userId, string documentId, CancellationToken cancellationToken = default);
}
