// Copyright (c) Microsoft. All rights reserved.
// ReSharper disable InconsistentNaming

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

internal static class Program
{
    public static async Task Main()
    {
        var memory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
            .Build<MemoryServerless>();

        memory.Orchestrator.AddHandler<MyHandler>("my_step");

        await memory.ImportDocumentAsync("sample-Wikipedia-Moon.txt", steps: ["my_step"]);

        /* Output:

        ** My handler is working **
        Index: default
        Document Id: 5b80b15a93554407a9abf2ecb3e5fbd6202402270203556813100
        Steps: my_step
        Remaining Steps: my_step

         */
    }
}

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
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        /* ... your custom ...
         * ... handler ...
         * ... business logic ... */

        Console.WriteLine("** My handler is working ** ");

        Console.WriteLine("Index: " + pipeline.Index);
        Console.WriteLine("Document Id: " + pipeline.DocumentId);
        Console.WriteLine("Steps: " + string.Join(", ", pipeline.Steps));
        Console.WriteLine("Remaining Steps: " + string.Join(", ", pipeline.RemainingSteps));

        // Remove this - here only to avoid build errors
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        return (true, pipeline);
    }
}
