// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.ContentStorage;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

public abstract class BaseOrchestrator : IPipelineOrchestrator, IDisposable
{
    protected const string StatusFile = "__pipeline_status.json";

    protected IContentStorage ContentStorage { get; private set; }
    protected ILogger<BaseOrchestrator> Log { get; private set; }
    protected CancellationTokenSource CancellationTokenSource { get; private set; }
    protected IMimeTypeDetection MimeTypeDetection { get; private set; }

    protected BaseOrchestrator(
        IContentStorage contentStorage,
        IMimeTypeDetection? mimeTypeDetection = null,
        ILogger<BaseOrchestrator>? log = null)
    {
        this.MimeTypeDetection = mimeTypeDetection ?? new MimeTypesDetection();
        this.ContentStorage = contentStorage;
        this.Log = log ?? NullLogger<BaseOrchestrator>.Instance;
        this.CancellationTokenSource = new CancellationTokenSource();
    }

    ///<inheritdoc />
    public abstract Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public abstract Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public abstract Task RunPipelineAsync(DataPipeline pipeline, CancellationToken cancellationToken = default);

    ///<inheritdoc />
    public DataPipeline PrepareNewFileUploadPipeline(string id, string userId, IEnumerable<string> vaultIds)
    {
        return this.PrepareNewFileUploadPipeline(id, userId, vaultIds, new List<IFormFile>());
    }

    ///<inheritdoc />
    public DataPipeline PrepareNewFileUploadPipeline(
        string id,
        string userId,
        IEnumerable<string> vaultIds,
        IEnumerable<IFormFile> filesToUpload)
    {
        var pipeline = new DataPipeline
        {
            Id = id,
            UserId = userId,
            VaultIds = vaultIds.ToList(),
            Creation = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
            FilesToUpload = filesToUpload.ToList(),
        };

        pipeline.Validate();

        return pipeline;
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
        BinaryData content = await this.ReadFileAsync(pipeline, fileName, cancellationToken).ConfigureAwait(false);
        return content.ToString();
    }

    ///<inheritdoc />
    public Task<BinaryData> ReadFileAsync(DataPipeline pipeline, string fileName, CancellationToken cancellationToken = default)
    {
        return this.ContentStorage.ReadFileAsync(pipeline.Id, fileName, cancellationToken);
    }

    ///<inheritdoc />
    public Task WriteTextFileAsync(DataPipeline pipeline, string fileName, string fileContent, CancellationToken cancellationToken = default)
    {
        return this.WriteFileAsync(pipeline, fileName, BinaryData.FromString(fileContent), cancellationToken);
    }

    ///<inheritdoc />
    public Task WriteFileAsync(DataPipeline pipeline, string fileName, BinaryData fileContent, CancellationToken cancellationToken = default)
    {
        return this.ContentStorage.WriteStreamAsync(
            pipeline.Id,
            fileName,
            fileContent.ToStream(),
            cancellationToken);
    }

    ///<inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task UploadFilesAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        if (pipeline.UploadComplete)
        {
            this.Log.LogDebug("Upload complete");
            return;
        }

        await this.ContentStorage.CreateDirectoryAsync(pipeline.Id, cancellationToken).ConfigureAwait(false);
        await this.UploadFormFilesAsync(pipeline, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Update the status file, throwing an exception if the write fails.
    /// </summary>
    /// <param name="pipeline">Pipeline data</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <param name="ignoreExceptions">Whether to throw exceptions or just log them</param>
    protected async Task UpdatePipelineStatusAsync(DataPipeline pipeline, CancellationToken cancellationToken, bool ignoreExceptions = false)
    {
        this.Log.LogInformation("Saving pipeline status to {0}/{1}", pipeline.Id, StatusFile);
        try
        {
            await this.ContentStorage.WriteTextFileAsync(
                    pipeline.Id,
                    StatusFile,
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
        this.Log.LogDebug("Uploading {0} files, pipeline {1}", pipeline.FilesToUpload.Count, pipeline.Id);

        await this.ContentStorage.CreateDirectoryAsync(pipeline.Id, cancellationToken).ConfigureAwait(false);

        var containerName = pipeline.Id;

        foreach (IFormFile file in pipeline.FilesToUpload)
        {
            if (string.Equals(file.FileName, StatusFile, StringComparison.OrdinalIgnoreCase))
            {
                this.Log.LogError("Invalid file name, upload not supported: {0}", file.FileName);
                continue;
            }

            this.Log.LogInformation("Uploading file: {0}", file.FileName);
            var size = await this.ContentStorage.WriteStreamAsync(containerName, file.FileName, file.OpenReadStream(), cancellationToken).ConfigureAwait(false);
            pipeline.Files.Add(new DataPipeline.FileDetails
            {
                Name = file.FileName,
                Size = size,
                Type = this.MimeTypeDetection.GetFileType(file.FileName),
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
