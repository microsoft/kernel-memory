// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel.AI.Embeddings;

namespace Microsoft.KernelMemory.Pipeline;

public class InProcessPipelineOrchestrator : BaseOrchestrator
{
    private readonly Dictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.InvariantCultureIgnoreCase);

    public InProcessPipelineOrchestrator(
        IContentStorage contentStorage,
        List<ITextEmbeddingGeneration> embeddingGenerators,
        List<IVectorDb> vectorDbs,
        ITextGeneration textGenerator,
        KernelMemoryConfig? config = null,
        IMimeTypeDetection? mimeTypeDetection = null,
        ILogger<InProcessPipelineOrchestrator>? log = null)
        : base(contentStorage, embeddingGenerators, vectorDbs, textGenerator, mimeTypeDetection, config, log)
    {
    }

    ///<inheritdoc />
    public override Task AddHandlerAsync(
        IPipelineStepHandler handler,
        CancellationToken cancellationToken = default)
    {
        this.AddHandler(handler);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronous (queue less) version of AddHandlerAsync. Register a pipeline handler.
    /// If a handler for the same step name already exists, it gets replaced.
    /// </summary>
    /// <param name="handler">Pipeline handler instance</param>
    public void AddHandler(IPipelineStepHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "The handler is NULL");
        }

        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (this._handlers.ContainsKey(handler.StepName))
        {
            throw new ArgumentException($"There is already a handler for step '{handler.StepName}'");
        }

        this._handlers[handler.StepName] = handler;
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

        if (this._handlers.ContainsKey(handler.StepName)) { return; }

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

        await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);

        while (!pipeline.Complete)
        {
            string currentStepName = pipeline.RemainingSteps.First();

            if (!this._handlers.TryGetValue(currentStepName, out var stepHandler))
            {
                throw new OrchestrationException($"No handlers found for step '{currentStepName}'");
            }

            // Run handler
            (bool success, DataPipeline updatedPipeline) = await stepHandler
                .InvokeAsync(pipeline, this.CancellationTokenSource.Token)
                .ConfigureAwait(false);
            if (success)
            {
                pipeline = updatedPipeline;
                pipeline.LastUpdate = DateTimeOffset.UtcNow;
                this.Log.LogInformation("Handler '{0}' processed pipeline '{1}/{2}' successfully", currentStepName, pipeline.Index, pipeline.DocumentId);
                pipeline.MoveToNextStep();
                await this.UpdatePipelineStatusAsync(pipeline, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                this.Log.LogError("Handler '{0}' failed to process pipeline '{1}/{2}'", currentStepName, pipeline.Index, pipeline.DocumentId);
                throw new OrchestrationException($"Pipeline error, step {currentStepName} failed");
            }
        }

        await this.CleanUpAfterCompletionAsync(pipeline, cancellationToken).ConfigureAwait(false);

        this.Log.LogInformation("Pipeline '{0}/{1}' complete", pipeline.Index, pipeline.DocumentId);
    }
}
