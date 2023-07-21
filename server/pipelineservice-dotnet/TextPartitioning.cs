// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.Services.SemanticMemory.PipelineService;

public class TextPartitioning : IHostedService, IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly TextPartitioningHandler _handler;

    public TextPartitioning(
        string stepName,
        IPipelineOrchestrator orchestrator,
        TextPartitioningHandler handler)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._handler = handler;
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
