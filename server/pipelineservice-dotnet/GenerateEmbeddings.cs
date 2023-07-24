// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.Services.SemanticMemory.PipelineService;

public class GenerateEmbeddings : IHostedService, IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly GenerateEmbeddingsHandler _handler;
    private readonly Dictionary<string, object> _embeddingGenerators;
    private readonly ILogger<GenerateEmbeddings> _log;

    public GenerateEmbeddings(
        string stepName,
        SKMemoryConfig configuration,
        IPipelineOrchestrator orchestrator,
        GenerateEmbeddingsHandler handler,
        ILogger<GenerateEmbeddings>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._handler = handler;
        this._log = log ?? NullLogger<GenerateEmbeddings>.Instance;

        // Setup the active embedding generators config, strongly typed
        this._embeddingGenerators = configuration.GetHandlerConfig<EmbeddingGenerationConfig>(stepName, "EmbeddingGeneration").GetActiveGeneratorsTypedConfig(log);
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return this._orchestrator.AddHandlerAsync(this, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        return this._handler.InvokeAsync(pipeline, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return this._orchestrator.StopAllPipelinesAsync();
    }
}
