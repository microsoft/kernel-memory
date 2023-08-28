// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticMemory.AI;
using Microsoft.SemanticMemory.ContentStorage;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.MemoryStorage;

namespace Microsoft.SemanticMemory.Pipeline;

public abstract class BaseOrchestrator : IPipelineOrchestrator, IDisposable
{
    private readonly List<ISemanticMemoryVectorDb> _vectorDbs;
    private readonly List<ITextEmbeddingGeneration> _embeddingGenerators;
    private readonly ITextGeneration _textGenerator;
    private readonly List<string> _defaultIngestionSteps;

    protected IContentStorage ContentStorage { get; private set; }
    protected ILogger<BaseOrchestrator> Log { get; private set; }
    protected CancellationTokenSource CancellationTokenSource { get; private set; }
    protected IMimeTypeDetection MimeTypeDetection { get; private set; }

    protected BaseOrchestrator(
        IContentStorage contentStorage,
        List<ITextEmbeddingGeneration> embeddingGenerators,
        List<ISemanticMemoryVectorDb> vectorDbs,
        ITextGeneration textGenerator,
        IMimeTypeDetection? mimeTypeDetection = null,
        SemanticMemoryConfig? config = null,
        ILogger<BaseOrchestrator>? log = null)
    {
        this.Log = log ?? DefaultLogger<BaseOrchestrator>.Instance;
        this.MimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
        this._defaultIngestionSteps = (config ?? new SemanticMemoryConfig()).DataIngestion.GetDefaultStepsOrDefaults();

        this.ContentStorage = contentStorage;
        this.CancellationTokenSource = new CancellationTokenSource();
        this._embeddingGenerators = embeddingGenerators;
        this._vectorDbs = vectorDbs;
        this._textGenerator = textGenerator;

        if (embeddingGenerators?.Count == 0)
        {
            this.Log.LogWarning("No embedding generators available");
        }

        if (vectorDbs?.Count == 0)
        {
            this.Log.LogWarning("No vector DBs available");
        }
    }

    ///<inheritdoc />
    public abstract Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public abstract Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public abstract Task RunPipelineAsync(DataPipeline pipeline, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public async Task<string> ImportDocumentAsync(string index, DocumentUploadRequest uploadRequest, CancellationToken cancellationToken = default)
    {
        this.Log.LogInformation("Queueing upload of {0} files for further processing [request {1}]", uploadRequest.Files.Count, uploadRequest.DocumentId);

        var pipeline = this.PrepareNewDocumentUpload(
            index: index,
            documentId: uploadRequest.DocumentId,
            uploadRequest.Tags,
            uploadRequest.Files);

        if (uploadRequest.Steps.Count > 0)
        {
            foreach (var step in uploadRequest.Steps)
            {
                pipeline.Then(step);
            }
        }
        else
        {
            foreach (var step in this._defaultIngestionSteps)
            {
                pipeline.Then(step);
            }
        }

        pipeline.Build();

        try
        {
            await this.RunPipelineAsync(pipeline, cancellationToken).ConfigureAwait(false);
            return pipeline.DocumentId;
        }
        catch (Exception e)
        {
            this.Log.LogError(e, "Pipeline start failed");
            throw;
        }
    }

    ///<inheritdoc />
    public DataPipeline PrepareNewDocumentUpload(
        string index,
        string documentId,
        TagCollection tags,
        IEnumerable<DocumentUploadRequest.UploadedFile>? filesToUpload = null)
    {
        filesToUpload ??= new List<DocumentUploadRequest.UploadedFile>();

        var pipeline = new DataPipeline
        {
            Index = IndexExtensions.CleanName(index),
            DocumentId = documentId,
            Tags = tags,
            Creation = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
            FilesToUpload = filesToUpload.ToList(),
        };

        pipeline.Validate();

        return pipeline;
    }

    ///<inheritdoc />
    public async Task<DataPipeline?> ReadPipelineStatusAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        var dirPath = this.ContentStorage.JoinPaths(index, documentId);
        try
        {
            BinaryData? content = await (this.ContentStorage.ReadFileAsync(dirPath, Constants.PipelineStatusFilename, false, cancellationToken)
                .ConfigureAwait(false));
            return content == null ? null : JsonSerializer.Deserialize<DataPipeline>(content.ToString());
        }
        catch (ContentStorageFileNotFoundException)
        {
            return null;
        }
    }

    ///<inheritdoc />
    public async Task<DataPipelineStatus?> ReadPipelineSummaryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        var pipeline = await this.ReadPipelineStatusAsync(index: index, documentId: documentId, cancellationToken).ConfigureAwait(false);
        return pipeline?.ToDataPipelineStatus();
    }

    ///<inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        DataPipeline? pipeline = await this.ReadPipelineStatusAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
        return pipeline != null && pipeline.Complete;
    }

    ///<inheritdoc />
    public Task StopAllPipelinesAsync()
    {
        this.CancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    ///<inheritdoc />
    public Task<BinaryData> ReadFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default)
    {
        var path = this.ContentStorage.JoinPaths(pipeline.Index, pipeline.DocumentId);
        return this.ContentStorage.ReadFileAsync(path, fileName, true, cancellationToken);
    }

    ///<inheritdoc />
    public async Task<string> ReadTextFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default)
    {
        BinaryData content = await this.ReadFileAsync(pipeline, fileName, cancellationToken).ConfigureAwait(false);
        return content.ToString();
    }

    ///<inheritdoc />
    public Task WriteFileAsync(DataPipeline pipeline, string fileName, BinaryData fileContent, CancellationToken cancellationToken = default)
    {
        var dirPath = this.ContentStorage.JoinPaths(pipeline.Index, pipeline.DocumentId);
        return this.ContentStorage.WriteStreamAsync(
            dirPath,
            fileName,
            fileContent.ToStream(),
            cancellationToken);
    }

    ///<inheritdoc />
    public Task WriteTextFileAsync(DataPipeline pipeline, string fileName, string fileContent, CancellationToken cancellationToken = default)
    {
        return this.WriteFileAsync(pipeline, fileName, BinaryData.FromString(fileContent), cancellationToken);
    }

    ///<inheritdoc />
    public List<ITextEmbeddingGeneration> GetEmbeddingGenerators()
    {
        return this._embeddingGenerators;
    }

    ///<inheritdoc />
    public List<ISemanticMemoryVectorDb> GetVectorDbs()
    {
        return this._vectorDbs;
    }

    ///<inheritdoc />
    public ITextGeneration GetTextGenerator()
    {
        return this._textGenerator;
    }

    ///<inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task UploadFilesAsync(DataPipeline currentPipeline, CancellationToken cancellationToken = default)
    {
        if (currentPipeline.UploadComplete)
        {
            this.Log.LogDebug("Upload complete");
            return;
        }

        // If the folder contains the status of a previous execution,
        // capture it to run consolidation later, e.g. purging deprecated memory records.
        // Note: although not required, the list of executions to purge is ordered from oldest to most recent
        DataPipeline? previousPipeline = await this.ReadPipelineStatusAsync(currentPipeline.Index, currentPipeline.DocumentId, cancellationToken).ConfigureAwait(false);
        if (previousPipeline != null && previousPipeline.ExecutionId != currentPipeline.ExecutionId)
        {
            var dedupe = new HashSet<string>();
            foreach (var oldExecution in currentPipeline.PreviousExecutionsToPurge)
            {
                dedupe.Add(oldExecution.ExecutionId);
            }

            foreach (var oldExecution in previousPipeline.PreviousExecutionsToPurge)
            {
                if (dedupe.Contains(oldExecution.ExecutionId)) { continue; }

                // Reset the list to avoid wasting space with nested trees
                oldExecution.PreviousExecutionsToPurge = new List<DataPipeline>();

                currentPipeline.PreviousExecutionsToPurge.Add(oldExecution);
                dedupe.Add(oldExecution.ExecutionId);
            }

            // Reset the list to avoid wasting space with nested trees
            previousPipeline.PreviousExecutionsToPurge = new List<DataPipeline>();

            currentPipeline.PreviousExecutionsToPurge.Add(previousPipeline);
        }

        await this.UploadFormFilesAsync(currentPipeline, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Update the status file, throwing an exception if the write fails.
    /// </summary>
    /// <param name="pipeline">Pipeline data</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <param name="ignoreExceptions">Whether to throw exceptions or just log them</param>
    protected async Task UpdatePipelineStatusAsync(DataPipeline pipeline, CancellationToken cancellationToken, bool ignoreExceptions = false)
    {
        this.Log.LogDebug("Saving pipeline status to {0}/{1}", pipeline.DocumentId, Constants.PipelineStatusFilename);
        try
        {
            var dirPath = this.ContentStorage.JoinPaths(pipeline.Index, pipeline.DocumentId);
            await this.ContentStorage.WriteTextFileAsync(
                    dirPath,
                    Constants.PipelineStatusFilename,
                    ToJson(pipeline, true),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (ignoreExceptions)
            {
                // Note: log a warning but continue. When a message is retrieved from the queue, the first step
                //       is ensuring the state is consistent with the queue. Note that the state on disk cannot be
                //       fully trusted, and the queue represents the source of truth.
                this.Log.LogWarning(e, "Unable to save pipeline status, the status on disk will be fixed when the pipeline continues");
                return;
            }

            this.Log.LogError(e, "Unable to save pipeline status");
            throw;
        }
    }

    protected static string ToJson(object data, bool indented = false)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = indented });
    }

    private async Task UploadFormFilesAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        this.Log.LogDebug("Uploading {0} files, pipeline {1}", pipeline.FilesToUpload.Count, pipeline.DocumentId);

        await this.ContentStorage.CreateDirectoryAsync(pipeline.Index, cancellationToken).ConfigureAwait(false);

        var dirPath = this.ContentStorage.JoinPaths(pipeline.Index, pipeline.DocumentId);
        await this.ContentStorage.CreateDirectoryAsync(dirPath, cancellationToken).ConfigureAwait(false);

        foreach (DocumentUploadRequest.UploadedFile file in pipeline.FilesToUpload)
        {
            if (string.Equals(file.FileName, Constants.PipelineStatusFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.Log.LogError("Invalid file name, upload not supported: {0}", file.FileName);
                continue;
            }

            this.Log.LogDebug("Uploading file: {0}", file.FileName);
            var size = await this.ContentStorage.WriteStreamAsync(dirPath, file.FileName, file.FileContent, cancellationToken).ConfigureAwait(false);

            string mimeType = string.Empty;
            try
            {
                mimeType = this.MimeTypeDetection.GetFileType(file.FileName);
            }
            catch (NotSupportedException)
            {
                this.Log.LogWarning("File type not supported, the ingestion pipeline might skip it");
            }

            pipeline.Files.Add(new DataPipeline.FileDetails
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = file.FileName,
                Size = size,
                MimeType = mimeType,
            });

            this.Log.LogInformation("File uploaded: {0}, {1} bytes", file.FileName, size);
            pipeline.LastUpdate = DateTimeOffset.UtcNow;
        }

        await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.CancellationTokenSource.Dispose();
        }
    }
}
