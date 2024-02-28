// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.ContentStorage;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Pipeline;

public class InProcessPipelineOrchestrator : BaseOrchestrator
{
    private readonly Dictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Create a new instance of the synchronous orchestrator.
    /// </summary>
    /// <param name="contentStorage">Service used to store files</param>
    /// <param name="embeddingGenerators">Services used to generate embeddings during the ingestion</param>
    /// <param name="memoryDbs">Services where to store memory records</param>
    /// <param name="textGenerator">Service used to generate text, e.g. synthetic memory records</param>
    /// <param name="config">Global KM configuration</param>
    /// <param name="mimeTypeDetection">Service used to detect a file type</param>
    /// <param name="serviceProvider">Optional service provider to add handlers by type</param>
    /// <param name="log">Application logger</param>
    public InProcessPipelineOrchestrator(
        IContentStorage contentStorage,
        List<ITextEmbeddingGenerator> embeddingGenerators,
        List<IMemoryDb> memoryDbs,
        ITextGenerator textGenerator,
        KernelMemoryConfig? config = null,
        IMimeTypeDetection? mimeTypeDetection = null,
        IServiceProvider? serviceProvider = null,
        ILogger<InProcessPipelineOrchestrator>? log = null)
        : base(contentStorage, embeddingGenerators, memoryDbs, textGenerator, mimeTypeDetection, config, log)
    {
        this._serviceProvider = serviceProvider;
    }

    ///<inheritdoc />
    public override Task AddHandlerAsync(
        IPipelineStepHandler handler,
        CancellationToken cancellationToken = default)
    {
        this.AddHandler(handler);
        return Task.CompletedTask;
    }

    ///<inheritdoc />
    public override Task TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handler.StepName))
        {
            throw new ArgumentNullException(nameof(handler.StepName), "The step name is empty");
        }

        if (this._handlers.ContainsKey(handler.StepName)) { return Task.CompletedTask; }

        try
        {
#pragma warning disable CA1849 // AddHandler doesn't do any I/O
            this.AddHandler(handler);
#pragma warning restore CA1849
        }
        catch (ArgumentException)
        {
            // TODO: use a more specific exception
            // Ignore
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Register a pipeline handler. If a handler for the same step name already exists, it gets replaced.
    /// </summary>
    /// <param name="stepName">Name of the queue/step associated with the handler</param>
    /// <typeparam name="T">Handler class</typeparam>
    public void AddHandler<T>(string stepName) where T : IPipelineStepHandler
    {
        if (this._serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is undefined. Try using <.AddHandler(handler instance)> method instead.");
        }

        this.AddHandler(ActivatorUtilities.CreateInstance<T>(this._serviceProvider, stepName));
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
