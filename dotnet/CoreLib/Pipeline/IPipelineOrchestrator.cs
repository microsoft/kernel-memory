﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory.Pipeline;

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
    /// Upload a file and start the processing pipeline
    /// </summary>
    /// <param name="index">Index where memory is stored</param>
    /// <param name="uploadRequest">Details about the file and how to import it</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Import Id</returns>
    Task<string> ImportDocumentAsync(string index, DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new pipeline value object for files upload
    /// </summary>
    /// <param name="index">Index where memory is stored</param>
    /// <param name="documentId">Id of the pipeline instance. This value will persist throughout the pipeline and final data lineage used for citations.</param>
    /// <param name="tags">List of key-value pairs, used to organize and label the memories. E.g. "type", "category", etc. Multiple values per key are allowed.</param>
    /// <param name="filesToUpload">List of files provided before starting the pipeline, to be uploaded into the container before starting.</param>
    /// <returns>Pipeline representation</returns>
    DataPipeline PrepareNewDocumentUpload(string index, string documentId, TagCollection tags, IEnumerable<DocumentUploadRequest.UploadedFile>? filesToUpload = null);

    /// <summary>
    /// Start a new data pipeline execution
    /// </summary>
    /// <param name="pipeline">Pipeline to execute</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task RunPipelineAsync(DataPipeline pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the pipeline status from storage
    /// </summary>
    /// <param name="index">Index where memory is stored</param>
    /// <param name="documentId">Id of the document and pipeline execution instance</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Pipeline status if available</returns>
    Task<DataPipeline?> ReadPipelineStatusAsync(string index, string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the pipeline status from storage
    /// </summary>
    /// <param name="index">Index where memory is stored</param>
    /// <param name="documentId">Id of the document and pipeline execution instance</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Pipeline status if available</returns>
    Task<DataPipelineStatus?> ReadPipelineSummaryAsync(string index, string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a document ID exists in a user memory and is ready for usage.
    /// The logic checks if the uploaded document has been fully processed.
    /// When the document exists in storage but is not processed yet, the method returns False.
    /// </summary>
    /// <param name="index">Index where memory is stored</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>True if the document has been successfully uploaded and imported</returns>
    public Task<bool> IsDocumentReadyAsync(string index, string documentId, CancellationToken cancellationToken = default);

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
    /// Write a text file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteTextFileAsync(DataPipeline pipeline, string fileName, string fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a file from content storage
    /// </summary>
    /// <param name="pipeline">Pipeline containing the file</param>
    /// <param name="fileName">Name of the file to fetch</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task WriteFileAsync(DataPipeline pipeline, string fileName, BinaryData fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of embedding generators to use during the ingestion, e.g. to create
    /// multiple vectors.
    /// TODO: remove and inject dependency to handlers who need this
    /// </summary>
    List<ITextEmbeddingGeneration> GetEmbeddingGenerators();

    /// <summary>
    /// Get list of Vector DBs where to store embeddings.
    /// TODO: remove and inject dependency to handlers who need this
    /// </summary>
    List<IVectorDb> GetVectorDbs();

    /// <summary>
    /// Get the text generator used for prompts, synthetic data, answer generation, etc.
    /// TODO: remove and inject dependency to handlers who need this
    /// TODO: support multiple generators, for different tasks, with different cost/quality.
    /// </summary>
    /// <returns>Instance of the text generator</returns>
    ITextGeneration GetTextGenerator();

    /// <summary>
    /// Start an asynchronous job, via handlers, to delete a specified index
    /// from vector and content storage. This might be a long running
    /// operation, hence the use of queue/handlers.
    /// </summary>
    /// <param name="index">Optional index name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task StartIndexDeletionAsync(string? index = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start an asynchronous job, via handlers, to delete a specified document
    /// from memory, and update all derived memories. This might be a long running
    /// operation, hence the use of queue/handlers.
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="index">Optional index name</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    Task StartDocumentDeletionAsync(
        string documentId,
        string? index = null,
        CancellationToken cancellationToken = default);
}
