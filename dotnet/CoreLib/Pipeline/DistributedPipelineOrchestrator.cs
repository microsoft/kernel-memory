﻿// Copyright (c) Microsoft. All rights reserved.

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

/// <summary>
/// Design notes:
/// The complete pipeline state is persisted on disk, and is often too big to fit into a queue message.
/// The message in the queue contains only the Index name and Pipeline ID (aka Document ID), which are used to load the state from disk.
/// In order, the state on disk is updated **before** enqueuing a message, so that a dequeued message will always find a consistent state.
/// When enqueueing fails:
/// - while starting a new pipeline, the client should get an error
/// - while continuing a pipeline, the system should retry the current step (which must be designed to be idempotent)
/// - while ending a pipeline, same thing, the last step will be repeated (and should be idempotent).
/// </summary>
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
            this.Log.LogTrace("Step `{0}`: processing message received from queue", handler.StepName);
            var pipelinePointer = JsonSerializer.Deserialize<DataPipelinePointer>(msg);
            if (pipelinePointer == null)
            {
                this.Log.LogError("Pipeline pointer deserialization failed, queue `{0}`", handler.StepName);
                // Note: returning False, the message is put back in the queue and processed again, eventually this will be moved to the poison queue if available
                return false;
            }

            DataPipeline? pipeline = await this.ReadPipelineStatusAsync(pipelinePointer.Index, pipelinePointer.DocumentId, cancellationToken).ConfigureAwait(false);
            if (pipeline == null)
            {
                this.Log.LogError("Pipeline state load failed, queue `{0}`", handler.StepName);
                // Note: returning False, the message is put back in the queue and processed again, eventually this will be moved to the poison queue if available
                return false;
            }

            var currentStepName = pipeline.RemainingSteps.First();
            // IMPORTANT: This can occur in case an exception interrupted the previous attempt,
            // e.g. the pipeline state was saved but the system couldn't enqueue a message to proceed with the following step.
            if (currentStepName != handler.StepName)
            {
                this.Log.LogWarning("Pipeline state on disk is ahead, next step is `{0}`, while the previous step `{1}` is still in the queue. Rolling back one step",
                    currentStepName, handler.StepName);
                pipeline.RollbackToPreviousStep();
                await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);
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
            pipeline.LastUpdate = DateTimeOffset.UtcNow;

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
        if (pipeline.Complete)
        {
            this.Log.LogInformation("Pipeline '{0}' complete", pipeline.DocumentId);

            // Save the pipeline status. If this fails the system should retry the current step.
            await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            string nextStepName = pipeline.RemainingSteps.First();
            this.Log.LogInformation("Enqueueing pipeline '{0}' step '{1}'", pipeline.DocumentId, nextStepName);

            // Execute as much logic as possible before writing the new pipeline state to disk,
            // to reduce the chance of the persisted state to be out of sync.
            using IQueue queue = this._queueClientFactory.Build();
            await queue.ConnectToQueueAsync(nextStepName, QueueOptions.PublishOnly, cancellationToken).ConfigureAwait(false);

            // Save the pipeline status to disk.
            // IMPORTANT: If this fails with an exception the system will retry the "next" step stored on disk,
            // which is the current step just completed.
            await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);

            // Enqueue a pointer to the pipeline (the entire pipeline doc can be too big to fit)
            // IMPORTANT: If this fails with an exception the state on disk will be ahead, and the system will retry the step before.
            await queue.EnqueueAsync(ToJson(new DataPipelinePointer(pipeline)), cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion
}
