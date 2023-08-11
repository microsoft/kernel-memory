﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.ContentStorage;
using Microsoft.SemanticMemory.Core.Diagnostics;

namespace Microsoft.SemanticMemory.Core.Pipeline;

public class InProcessPipelineOrchestrator : BaseOrchestrator
{
    private readonly Dictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.InvariantCultureIgnoreCase);

    public InProcessPipelineOrchestrator(
        IContentStorage contentStorage,
        IServiceProvider serviceProvider,
        IMimeTypeDetection? mimeTypeDetection = null,
        ILogger<InProcessPipelineOrchestrator>? log = null)
        : base(contentStorage, serviceProvider, mimeTypeDetection, log)
    {
    }

    ///<inheritdoc />
    public override Task AddHandlerAsync(
        IPipelineStepHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "The handler is NULL");
        }

        if (this._handlers.ContainsKey(handler.StepName))
        {
            throw new ArgumentException($"There is already a handler for step '{handler.StepName}'");
        }

        this._handlers[handler.StepName] = handler;

        return Task.CompletedTask;
    }

    ///<inheritdoc />
    public override async Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "The handler is NULL");
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
    public override async Task RunPipelineAsync(
        DataPipeline pipeline,
        CancellationToken cancellationToken = default)
    {
        // Files must be uploaded before starting any other task
        await this.UploadFilesAsync(pipeline, cancellationToken).ConfigureAwait(false);

        await this.UpdatePipelineStatusAsync(pipeline, cancellationToken, ignoreExceptions: false).ConfigureAwait(false);

        while (!pipeline.Complete)
        {
            string currentStepName = pipeline.RemainingSteps.First();
            if (!this._handlers.ContainsKey(currentStepName))
            {
                throw new OrchestrationException($"No handlers found for step '{currentStepName}'");
            }

            // Run handler
            (bool success, DataPipeline updatedPipeline) = await this._handlers[currentStepName]
                .InvokeAsync(pipeline, this.CancellationTokenSource.Token)
                .ConfigureAwait(false);
            if (success)
            {
                pipeline = updatedPipeline;
                this.Log.LogInformation("Handler '{0}' processed pipeline '{1}' successfully", currentStepName, pipeline.DocumentId);
                pipeline.MoveToNextStep();
                await this.UpdatePipelineStatusAsync(pipeline, cancellationToken, ignoreExceptions: false).ConfigureAwait(false);
            }
            else
            {
                this.Log.LogError("Handler '{0}' failed to process pipeline '{1}'", currentStepName, pipeline.DocumentId);
                throw new OrchestrationException($"Pipeline error, step {currentStepName} failed");
            }
        }

        this.Log.LogInformation("Pipeline '{0}' complete", pipeline.DocumentId);
    }
}
