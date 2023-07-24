// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

public class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<GenerateEmbeddingsHandler> _log;

    public GenerateEmbeddingsHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<GenerateEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<GenerateEmbeddingsHandler>.Instance;
    }

    public string StepName { get; }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken).ConfigureAwait(false);

        foreach (var originalFile in pipeline.Files)
        {
            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in originalFile.GeneratedFiles)
            {
                var file = generatedFile.Value;

                switch (file.Type)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        // calc embeddings
                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be used to generate embedding, type not supported", file.Name);
                        continue;
                }
            }
        }

        return (true, pipeline);
    }
}
