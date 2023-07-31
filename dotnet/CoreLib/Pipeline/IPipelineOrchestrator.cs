// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticMemory.Client;

namespace Microsoft.SemanticMemory.Core.Pipeline;

public interface IPipelineOrchestrator
{
    /// <summary>
    /// Attach a handler for a specific task
    /// </summary>
    /// <param name="handler">Handler instance</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attach a handler for a specific task
    /// </summary>
    /// <param name="handler">Handler instance</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new pipeline value object for files upload
    /// </summary>
    /// <param name="documentId">Id of the pipeline instance. This value will persist throughout the pipeline and final data lineage used for citations.</param>
    /// <param name="userId">Primary user who the data belongs to. Other users, e.g. sharing, is not supported in the pipeline at this time.</param>
    /// <param name="tags">List of key-value pairs, used to organize and label the memories. E.g. "type", "category", etc. Multiple values per key are allowed.</param>
    /// <param name="filesToUpload">List of files provided before starting the pipeline, to be uploaded into the container before starting.</param>
    /// <returns>Pipeline representation</returns>
    DataPipeline PrepareNewFileUploadPipeline(string documentId, string userId, TagCollection tags, IEnumerable<IFormFile> filesToUpload);

    /// <summary>
    /// Create a new pipeline value object, with an empty list of files
    /// </summary>
    /// <param name="documentId">Id of the pipeline instance. This value will persist throughout the pipeline and final data lineage used for citations.</param>
    /// <param name="userId">Primary user who the data belongs to. Other users, e.g. sharing, is not supported in the pipeline at this time.</param>
    /// <param name="tags">List of key-value pairs, used to organize and label the memories. E.g. "type", "category", etc. Multiple values per key are allowed.</param>
    /// <returns>Pipeline representation</returns>
    DataPipeline PrepareNewFileUploadPipeline(string documentId, string userId, TagCollection tags);

    /// <summary>
    /// Start a new data pipeline execution
    /// </summary>
    /// <param name="pipeline">Pipeline to execute</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task RunPipelineAsync(DataPipeline pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop all the pipelines in progress
    /// </summary>
    Task StopAllPipelinesAsync();

    /// <summary>
    /// Fetch a file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task<BinaryData> ReadFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task<string> ReadTextFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteFileAsync(DataPipeline pipeline, string fileName, BinaryData fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteTextFileAsync(DataPipeline pipeline, string fileName, string fileContent, CancellationToken cancellationToken = default);
}
