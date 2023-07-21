// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticKernel.SemanticMemory.Core20;

namespace Microsoft.SemanticKernel.SemanticMemory.Core;

public class SemanticMemoryClient : ISemanticMemoryClient
{
    private readonly Lazy<Task<InProcessPipelineOrchestrator>> _inProcessOrchestrator = new(BuildInProcessOrchestratorAsync);

    private Task<InProcessPipelineOrchestrator> Orchestrator
    {
        get { return this._inProcessOrchestrator.Value; }
    }

    public Task ImportFileAsync(string file, ImportFileOptions options)
    {
        return this.ImportFilesInternalAsync(new[] { file }, options);
    }

    public Task ImportFilesAsync(string[] files, ImportFileOptions options)
    {
        return this.ImportFilesInternalAsync(files, options);
    }

    private async Task ImportFilesInternalAsync(string[] files, ImportFileOptions options)
    {
        options.Sanitize();
        options.Validate();

        InProcessPipelineOrchestrator orchestrator = await this.Orchestrator.ConfigureAwait(false);

        var pipeline = orchestrator
            .PrepareNewFileUploadPipeline(options.RequestId, options.UserId, options.VaultIds);

        // Include all files
        for (int index = 0; index < files.Length; index++)
        {
            string? file = files[index];
            pipeline.AddUploadFile($"file{index + 1}", file, file);
        }

        // TODO: .Then("index")
        pipeline.Then("extract").Then("partition").Build();

        // Execute pipeline
        await orchestrator.RunPipelineAsync(pipeline).ConfigureAwait(false);
    }

    private static async Task<InProcessPipelineOrchestrator> BuildInProcessOrchestratorAsync()
    {
        IServiceProvider services = AppBuilder.Build().Services;
        var orchestrator = services.GetService<InProcessPipelineOrchestrator>();
        if (orchestrator == null)
        {
            throw new ArgumentNullException(nameof(orchestrator),
                $"Unable to instantiate {typeof(InProcessPipelineOrchestrator)} with AppBuilder");
        }

        TextExtractionHandler textExtraction = new("extract", orchestrator);
        await orchestrator.AddHandlerAsync(textExtraction).ConfigureAwait(false);

        TextPartitioningHandler textPartitioning = new("partition", orchestrator);
        await orchestrator.AddHandlerAsync(textPartitioning).ConfigureAwait(false);

        return orchestrator;
    }
}
