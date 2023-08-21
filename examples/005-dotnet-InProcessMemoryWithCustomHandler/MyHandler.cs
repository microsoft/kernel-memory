// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Pipeline;

public class MyHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<MyHandler> _log;

    public MyHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<MyHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<MyHandler>.Instance;
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        /* ... your custom ...
         * ... handler ...
         * ... business logic ... */

        Console.WriteLine("My handler is working");

        // Remove this - here only to avoid build errors
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
