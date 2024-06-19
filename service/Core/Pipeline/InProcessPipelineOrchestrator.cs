// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.MemoryStorage;

namespace Microsoft.KernelMemory.Pipeline;

[Experimental("KMEXP04")]
public sealed class InProcessPipelineOrchestrator : BaseOrchestrator
{
    private readonly Dictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Create a new instance of the synchronous orchestrator.
    /// </summary>
    /// <param name="documentStorage">Service used to store files</param>
    /// <param name="embeddingGenerators">Services used to generate embeddings during the ingestion</param>
    /// <param name="memoryDbs">Services where to store memory records</param>
    /// <param name="textGenerator">Service used to generate text, e.g. synthetic memory records</param>
    /// <param name="mimeTypeDetection">Service used to detect a file type</param>
    /// <param name="serviceProvider">Optional service provider to add handlers by type</param>
    /// <param name="config">Global KM configuration</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public InProcessPipelineOrchestrator(
        IDocumentStorage documentStorage,
        List<ITextEmbeddingGenerator> embeddingGenerators,
        List<IMemoryDb> memoryDbs,
        ITextGenerator textGenerator,
        IMimeTypeDetection? mimeTypeDetection = null,
        IServiceProvider? serviceProvider = null,
        KernelMemoryConfig? config = null,
        ILoggerFactory? loggerFactory = null)
        : base(documentStorage, embeddingGenerators, memoryDbs, textGenerator, mimeTypeDetection, config, loggerFactory?.CreateLogger<InProcessPipelineOrchestrator>())
    {
        this._serviceProvider = serviceProvider;
    }

    ///<inheritdoc />
    public override List<string> HandlerNames
    {
        get
        {
            return this._handlers.Keys.OrderBy(x => x).ToList();
        }
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
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(handler.StepName, nameof(handler.StepName), "The step name is empty");

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
    /// Register a pipeline handler.
    /// </summary>
    /// <param name="config">Handler type configuration</param>
    /// <param name="stepName">Pipeline step name</param>
    public void AddSynchronousHandler(HandlerConfig config, string stepName)
    {
        if (HandlerTypeLoader.TryGetHandlerType(config, out var handlerType))
        {
            this.AddHandler(handlerType, stepName);
        }
    }

    /// <summary>
    /// Register a pipeline handler.
    /// </summary>
    /// <param name="handlerType">Handler class</param>
    /// <param name="stepName">Name of the queue/step associated with the handler</param>
    public void AddHandler(Type handlerType, string stepName)
    {
        if (this._serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is undefined. Try using <.AddHandler(handler instance)> method instead.");
        }

        var handler = ActivatorUtilities.CreateInstance(this._serviceProvider, handlerType, stepName);
        if (handler is not IPipelineStepHandler)
        {
            throw new InvalidOperationException($"Type '{handlerType}' is not valid: {nameof(IPipelineStepHandler)} not implemented.");
        }

        this.AddHandler((IPipelineStepHandler)handler);
    }

    /// <summary>
    /// Synchronous (queue less) version of AddHandlerAsync. Register a pipeline handler.
    /// If a handler for the same step name already exists, it gets replaced.
    /// </summary>
    /// <param name="handler">Pipeline handler instance</param>
    public void AddHandler(IPipelineStepHandler handler)
    {
        ArgumentNullExceptionEx.ThrowIfNull(handler, nameof(handler), "The handler is NULL");
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(handler.StepName, nameof(handler.StepName), "The step name is empty");

        if (!this._handlers.TryAdd(handler.StepName, handler))
        {
            throw new ArgumentException($"There is already a handler for step '{handler.StepName}'");
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
