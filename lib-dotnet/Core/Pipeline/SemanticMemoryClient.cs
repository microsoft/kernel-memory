// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

public class SemanticMemoryClient
{
    private readonly IPipelineOrchestrator _orchestrator;

    private readonly IPipelineStepHandler? _textExtraction = null;

    public SemanticMemoryClient(IServiceProvider services) : this(services.GetService<IPipelineOrchestrator>()!)
    {
        this._textExtraction = new TextExtractionHandler("extract", this._orchestrator);
    }

    public SemanticMemoryClient(IPipelineOrchestrator orchestrator)
    {
        this._orchestrator = orchestrator;
    }

    public Task ImportFileAsync(string file, string userid, string vaultId)
    {
        return this.ImportFilesAsync(new[] { file }, userid, new[] { vaultId });
    }

    public Task ImportFileAsync(string file, string userid, string[] vaults)
    {
        return this.ImportFilesAsync(new[] { file }, userid, vaults);
    }

    public Task ImportFileAsync(string[] files, string userid, string vaultId)
    {
        return this.ImportFilesAsync(files, userid, new[] { vaultId });
    }

    public Task ImportFilesAsync(string[] files, string userid, string vaultId)
    {
        return this.ImportFilesAsync(files, userid, new[] { vaultId });
    }

    public async Task ImportFilesAsync(string[] files, string userid, string[] vaults)
    {
        // Attach handlers
        await this._orchestrator.TryAddHandlerAsync(this._textExtraction!).ConfigureAwait(false);

        var pipeline = this._orchestrator
            .PrepareNewFileUploadPipeline(Guid.NewGuid().ToString("D"), userid, vaults);

        // Include all files
        for (int index = 0; index < files.Length; index++)
        {
            string? file = files[index];
            pipeline.AddUploadFile($"file{index + 1}", file, file);
        }

        pipeline.Then("extract")
            // .Then("partition")
            // .Then("index")
            .Build();

        // Execute pipeline
        await this._orchestrator.RunPipelineAsync(pipeline).ConfigureAwait(false);
    }
}
