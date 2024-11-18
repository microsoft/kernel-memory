// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.Pipeline;

public interface IPipelineStepHandler
{
    /// <summary>
    /// Name of the pipeline step assigned to the handler
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Method invoked by kernel memory orchestrators to process a pipeline.
    /// The method is invoked only when the next step in the pipeline matches
    /// with the name handled by the handler. See <see cref="IPipelineOrchestrator.AddHandlerAsync"/>
    /// </summary>
    /// <param name="pipeline">Pipeline status</param>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Whether the pipeline step has been processed successfully, and the new pipeline status to use moving forward</returns>
    Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default);
}
