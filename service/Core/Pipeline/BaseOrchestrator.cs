// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Pipeline;

public abstract class BaseOrchestrator : IPipelineOrchestrator, IDisposable
{
    private static readonly JsonSerializerOptions s_indentedJsonOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_notIndentedJsonOptions = new() { WriteIndented = false };

    private readonly List<IMemoryDb> _memoryDbs;
    private readonly List<ITextEmbeddingGenerator> _embeddingGenerators;
    private readonly ITextGenerator _textGenerator;
    private readonly List<string> _defaultIngestionSteps;
    private readonly IContentStorage _contentStorage;
    private readonly IMimeTypeDetection _mimeTypeDetection;

    protected ILogger<BaseOrchestrator> Log { get; private set; }
    protected CancellationTokenSource CancellationTokenSource { get; private set; }

    protected BaseOrchestrator(
        IContentStorage contentStorage,
        List<ITextEmbeddingGenerator> embeddingGenerators,
        List<IMemoryDb> memoryDbs,
        ITextGenerator textGenerator,
        IMimeTypeDetection? mimeTypeDetection = null,
        KernelMemoryConfig? config = null,
        ILogger<BaseOrchestrator>? log = null)
    {
        config ??= new KernelMemoryConfig();

        this.Log = log ?? DefaultLogger<BaseOrchestrator>.Instance;
        this._defaultIngestionSteps = config.DataIngestion.GetDefaultStepsOrDefaults();
        this.EmbeddingGenerationEnabled = config.DataIngestion.EmbeddingGenerationEnabled;
        this._contentStorage = contentStorage;
        this._embeddingGenerators = embeddingGenerators;
        this._memoryDbs = memoryDbs;
        this._textGenerator = textGenerator;

        this._mimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
        this.CancellationTokenSource = new CancellationTokenSource();

        if (this.EmbeddingGenerationEnabled && embeddingGenerators.Count == 0)
        {
            this.Log.LogWarning("No embedding generators available");
        }

        if (memoryDbs.Count == 0)
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
            Index = index,
            DocumentId = documentId,
            Tags = tags,
            FilesToUpload = filesToUpload.ToList(),
        };

        pipeline.Validate();

        return pipeline;
    }

    ///<inheritdoc />
    public async Task<DataPipeline?> ReadPipelineStatusAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        index = IndexExtensions.CleanName(index);
        try
        {
            BinaryData? content = await (this._contentStorage.ReadFileAsync(index, documentId, Constants.PipelineStatusFilename, false, cancellationToken)
                .ConfigureAwait(false));
            return content == null ? null : JsonSerializer.Deserialize<DataPipeline>(content.ToString().RemoveBOM().Trim());
        }
        catch (ContentStorageFileNotFoundException)
        {
            return null;
        }
    }

    ///<inheritdoc />
    public async Task<DataPipelineStatus?> ReadPipelineSummaryAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        DataPipeline? pipeline = await this.ReadPipelineStatusAsync(index: index, documentId: documentId, cancellationToken).ConfigureAwait(false);
        return pipeline?.ToDataPipelineStatus();
    }

    ///<inheritdoc />
    public async Task<bool> IsDocumentReadyAsync(string index, string documentId, CancellationToken cancellationToken = default)
    {
        DataPipeline? pipeline = await this.ReadPipelineStatusAsync(index: index, documentId, cancellationToken).ConfigureAwait(false);
        return pipeline != null && pipeline.Complete && pipeline.Files.Count > 0;
    }

    ///<inheritdoc />
    public Task StopAllPipelinesAsync()
    {
        this.CancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    ///<inheritdoc />
    public async Task<string> ReadTextFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default)
    {
        return (await this.ReadFileAsync(pipeline, fileName, cancellationToken).ConfigureAwait(false)).ToString();
    }

    ///<inheritdoc />
    public Task<BinaryData> ReadFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default)
    {
        return this._contentStorage.ReadFileAsync(pipeline.Index, pipeline.DocumentId, fileName, true, cancellationToken);
    }

    ///<inheritdoc />
    public Task WriteTextFileAsync(DataPipeline pipeline, string fileName, string fileContent, CancellationToken cancellationToken = default)
    {
        return this.WriteFileAsync(pipeline, fileName, new BinaryData(fileContent), cancellationToken);
    }

    ///<inheritdoc />
    public Task WriteFileAsync(DataPipeline pipeline, string fileName, BinaryData fileContent, CancellationToken cancellationToken = default)
    {
        return this._contentStorage.WriteFileAsync(pipeline.Index, pipeline.DocumentId, fileName, fileContent.ToStream(), cancellationToken);
    }

    ///<inheritdoc />
    public bool EmbeddingGenerationEnabled { get; }

    ///<inheritdoc />
    public List<ITextEmbeddingGenerator> GetEmbeddingGenerators()
    {
        return this._embeddingGenerators;
    }

    ///<inheritdoc />
    public List<IMemoryDb> GetMemoryDbs()
    {
        return this._memoryDbs;
    }

    ///<inheritdoc />
    public ITextGenerator GetTextGenerator()
    {
        return this._textGenerator;
    }

    ///<inheritdoc />
    public Task StartIndexDeletionAsync(string? index = null, CancellationToken cancellationToken = default)
    {
        DataPipeline pipeline = PrepareIndexDeletion(index: index);
        return this.RunPipelineAsync(pipeline, cancellationToken);
    }

    ///<inheritdoc />
    public Task StartDocumentDeletionAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        DataPipeline pipeline = PrepareDocumentDeletion(index: index, documentId: documentId);
        return this.RunPipelineAsync(pipeline, cancellationToken);
    }

    ///<inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// If the pipeline asked to delete a document or an index, there might be some files
    /// left over in the storage, such as the status file that we wish to delete to keep
    /// the storage clean. We try to delete what is left, ignoring exceptions.
    /// </summary>
    protected async Task CleanUpAfterCompletionAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1031 // catch all by design
        if (pipeline.IsDocumentDeletionPipeline())
        {
            try
            {
                await this._contentStorage.DeleteDocumentDirectoryAsync(index: pipeline.Index, documentId: pipeline.DocumentId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.Log.LogError(e, "Error while trying to delete the document directory");
            }
        }

        if (pipeline.IsIndexDeletionPipeline())
        {
            try
            {
                await this._contentStorage.DeleteIndexDirectoryAsync(pipeline.Index, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.Log.LogError(e, "Error while trying to delete the index directory");
            }
        }
#pragma warning restore CA1031
    }

    protected static DataPipeline PrepareIndexDeletion(string? index)
    {
        var pipeline = new DataPipeline
        {
            Index = index!,
            DocumentId = string.Empty,
        };

        return pipeline.Then(Constants.PipelineStepsDeleteIndex).Build();
    }

    protected static DataPipeline PrepareDocumentDeletion(string? index, string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new KernelMemoryException("The document ID is empty");
        }

        var pipeline = new DataPipeline
        {
            Index = index!,
            DocumentId = documentId,
        };

        return pipeline.Then(Constants.PipelineStepsDeleteDocument).Build();
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
        DataPipeline? previousPipeline = await this.ReadPipelineStatusAsync(
            currentPipeline.Index, currentPipeline.DocumentId, cancellationToken).ConfigureAwait(false);
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
    protected async Task UpdatePipelineStatusAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        this.Log.LogDebug("Saving pipeline status to '{0}/{1}/{2}'", pipeline.Index, pipeline.DocumentId, Constants.PipelineStatusFilename);
        try
        {
            await this._contentStorage.WriteFileAsync(
                    pipeline.Index,
                    pipeline.DocumentId,
                    Constants.PipelineStatusFilename,
                    new BinaryData(ToJson(pipeline, true)).ToStream(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            this.Log.LogWarning(e, "Unable to save pipeline status");
            throw;
        }
    }

    protected static string ToJson(object data, bool indented = false)
    {
        return JsonSerializer.Serialize(data, indented ? s_indentedJsonOptions : s_notIndentedJsonOptions);
    }

    private async Task UploadFormFilesAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        this.Log.LogDebug("Uploading {0} files, pipeline '{1}/{2}'", pipeline.FilesToUpload.Count, pipeline.Index, pipeline.DocumentId);

        await this._contentStorage.CreateIndexDirectoryAsync(pipeline.Index, cancellationToken).ConfigureAwait(false);
        await this._contentStorage.CreateDocumentDirectoryAsync(pipeline.Index, pipeline.DocumentId, cancellationToken).ConfigureAwait(false);

        foreach (DocumentUploadRequest.UploadedFile file in pipeline.FilesToUpload)
        {
            if (string.Equals(file.FileName, Constants.PipelineStatusFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.Log.LogError("Invalid file name, upload not supported: {0}", file.FileName);
                continue;
            }

            // Read the value before the stream is closed (would throw an exception otherwise)
            var fileSize = file.FileContent.Length;

            this.Log.LogDebug("Uploading file '{0}', size {1} bytes", file.FileName, fileSize);
            await this._contentStorage.WriteFileAsync(pipeline.Index, pipeline.DocumentId, file.FileName, file.FileContent, cancellationToken).ConfigureAwait(false);

            string mimeType = string.Empty;
            try
            {
                mimeType = this._mimeTypeDetection.GetFileType(file.FileName);
            }
            catch (NotSupportedException)
            {
                this.Log.LogWarning("File type not supported, the ingestion pipeline might skip it");
            }

            pipeline.Files.Add(new DataPipeline.FileDetails
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = file.FileName,
                Size = fileSize,
                MimeType = mimeType,
                Tags = pipeline.Tags,
            });

            this.Log.LogInformation("File uploaded: {0}, {1} bytes", file.FileName, fileSize);
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
