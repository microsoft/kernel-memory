// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.Client;

public interface ISemanticMemoryClient
{
    /// <summary>
    /// Import a file into memory. The file can have tags and other details.
    /// </summary>
    /// <param name="file">Details of the file to import</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(Document file);

    /// <summary>
    /// Import multiple files into memory. Each file can have tags and other details.
    /// </summary>
    /// <param name="files">Details of the files to import</param>
    /// <returns>List of document IDs</returns>
    public Task<IList<string>> ImportFilesAsync(Document[] files);

    /// <summary>
    /// Import a file from disk into memory.
    /// </summary>
    /// <param name="fileName">Path and name of the file to import</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(string fileName);

    /// <summary>
    /// Import a files from disk into memory, with details such as tags and user ID.
    /// </summary>
    /// <param name="fileName">Path and name of the files to import</param>
    /// <param name="details">File details such as tags and user ID</param>
    /// <returns>Document ID</returns>
    public Task<string> ImportFileAsync(string fileName, DocumentDetails details);

    /// <summary>
    /// Search a user memory for an answer to the given query.
    /// TODO: add support for tags.
    /// </summary>
    /// <param name="userId">ID of the user's memory to search</param>
    /// <param name="query">Query/question to answer</param>
    /// <returns>Answer to the query, if possible</returns>
    public Task<MemoryAnswer> AskAsync(string userId, string query);

    /// <summary>
    /// Check if a document ID exists in a user memory.
    /// </summary>
    /// <param name="userId">ID of the user's memory to search</param>
    /// <param name="documentId">Document ID</param>
    /// <returns>True if the document has been successfully uploaded and imported</returns>
    public Task<bool> ExistsAsync(string userId, string documentId);
}
