// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

public class SaveEmbeddingsToAzureCognitiveSearchHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<SaveEmbeddingsToAzureCognitiveSearchHandler> _log;

    /// <summary>
    /// Note: stepName and other params are injected with DI, <see cref="DependencyInjection.UseHandler{THandler}"/>
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="configuration">Application settings</param>
    /// <param name="log">Application logger</param>
    public SaveEmbeddingsToAzureCognitiveSearchHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        SKMemoryConfig configuration,
        ILogger<SaveEmbeddingsToAzureCognitiveSearchHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<SaveEmbeddingsToAzureCognitiveSearchHandler>.Instance;

        this._log.LogInformation("Handler ready: {0}", stepName);
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
