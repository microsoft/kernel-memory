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
using Microsoft.SemanticMemory.MemoryStorage;
using Microsoft.SemanticMemory.Pipeline.Queue;

namespace Microsoft.SemanticMemory.Pipeline;

public class DistributedPipelineOrchestrator : BaseOrchestrator
{
    private readonly QueueClientFactory _queueClientFactory;

    private readonly Dictionary<string, IQueue> _queues = new(StringComparer.InvariantCultureIgnoreCase);

    public DistributedPipelineOrchestrator(
        IContentStorage contentStorage,
        IMimeTypeDetection mimeTypeDetection,
        QueueClientFactory queueClientFactory,
        List<ITextEmbeddingGeneration> embeddingGenerators,
        List<ISemanticMemoryVectorDb> vectorDbs,
        ITextGeneration textGenerator,
        SemanticMemoryConfig? config = null,
        ILogger<DistributedPipelineOrchestrator>? log = null)
        : base(contentStorage, embeddingGenerators, vectorDbs, textGenerator, mimeTypeDetection, config, log)
    {
        this._queueClientFactory = queueClientFactory;
    }

    ///<inheritdoc />
    public override async Task AddHandlerAsync(
        IPipelineStepHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "The handler is NULL");
        }

        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (this._queues.ContainsKey(handler.StepName))
        {
            throw new ArgumentException($"There is already a handler for step '{handler.StepName}'");
        }

        // Create a new queue client and start listening for messages
        this._queues[handler.StepName] = this._queueClientFactory.Build();
        this._queues[handler.StepName].OnDequeue(async msg =>
        {
            var pipeline = JsonSerializer.Deserialize<DataPipeline>(msg);

            if (pipeline == null)
            {
                this.Log.LogError("Pipeline deserialization failed, queue {0}`", handler.StepName);
                // Note: returning False, the message is put back in the queue and processed again, eventually this will be moved to the poison queue if available
                return false;
            }

            // This should never occur unless there's a bug
            var currentStepName = pipeline.RemainingSteps.First();
            if (currentStepName != handler.StepName)
            {
                this.Log.LogError("Pipeline state is inconsistent. Queue `{0}` should not contain a pipeline at step `{1}`", handler.StepName, currentStepName);
                // Note: returning False, the message is put back in the queue and processed again, eventually this will be moved to the poison queue if available
                return false;
            }

            return await this.RunPipelineStepAsync(pipeline, handler, this.CancellationTokenSource.Token).ConfigureAwait(false);
        });
        await this._queues[handler.StepName].ConnectToQueueAsync(handler.StepName, QueueOptions.PubSub, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    ///<inheritdoc />
    public override async Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "The handler is NULL");
        }

        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (this._queues.ContainsKey(handler.StepName)) { return; }

        try
        {
            await this.AddHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            // TODO: use a more specific exception
            // Ignore
        }
    }

    ///<inheritdoc />
    public override async Task RunPipelineAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // Files must be uploaded before starting any other task
        await this.UploadFilesAsync(pipeline, cancellationToken).ConfigureAwait(false);

        // In case the pipeline has no steps
        if (pipeline.Complete)
        {
            this.Log.LogInformation("Pipeline {0} complete", pipeline.DocumentId);
            return;
        }

        await this.MoveForwardAsync(pipeline, cancellationToken).ConfigureAwait(false);
    }

    #region private

    private async Task<bool> RunPipelineStepAsync(
        DataPipeline pipeline,
        IPipelineStepHandler handler,
        CancellationToken cancellationToken)
    {
        // Sync state on disk with state in the queue
        await this.UpdatePipelineStatusAsync(pipeline, cancellationToken, ignoreExceptions: false).ConfigureAwait(false);

        // In case the pipeline has no steps
        if (pipeline.Complete)
        {
            this.Log.LogInformation("Pipeline {0} complete", pipeline.DocumentId);
            // Note: returning True, the message is removed from the queue
            return true;
        }

        string currentStepName = pipeline.RemainingSteps.First();

        // Execute the business logic - exceptions are automatically handled by IQueue
        (bool success, DataPipeline updatedPipeline) = await handler.InvokeAsync(pipeline, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            pipeline = updatedPipeline;

            this.Log.LogInformation("Handler {0} processed pipeline {1} successfully", currentStepName, pipeline.DocumentId);
            pipeline.MoveToNextStep();
            await this.MoveForwardAsync(pipeline, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            this.Log.LogError("Handler {0} failed to process pipeline {1}", currentStepName, pipeline.DocumentId);
        }

        // Note: returning True, the message is removed from the queue
        // Note: returning False, the message is put back in the queue and processed again
        return success;
    }

    private async Task MoveForwardAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // Note: the pipeline state is persisted in two places:
        // * source of truth: in the queue (see the message enqueued)
        // * async copy: in the container together with files - this can be out of sync and is synchronized on dequeue

        if (pipeline.RemainingSteps.Count == 0)
        {
            this.Log.LogInformation("Pipeline '{0}' complete", pipeline.DocumentId);

            // Try to save the pipeline status
            await this.UpdatePipelineStatusAsync(pipeline, cancellationToken, ignoreExceptions: false).ConfigureAwait(false);
        }
        else
        {
            string nextStepName = pipeline.RemainingSteps.First();
            this.Log.LogInformation("Enqueueing pipeline '{0}' step '{1}'", pipeline.DocumentId, nextStepName);

            using IQueue queue = this._queueClientFactory.Build();
            await queue.ConnectToQueueAsync(nextStepName, QueueOptions.PublishOnly, cancellationToken).ConfigureAwait(false);
            await queue.EnqueueAsync(ToJson(pipeline), cancellationToken).ConfigureAwait(false);

            // Try to save the pipeline status
            await this.UpdatePipelineStatusAsync(pipeline, cancellationToken, ignoreExceptions: true).ConfigureAwait(false);
        }
    }

    #endregion
}
