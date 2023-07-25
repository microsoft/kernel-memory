// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticKernel.SemanticMemory.Core.Configuration;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for generating text embedding and saving them to the content storage.
/// </summary>
public class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly Dictionary<string, object> _embeddingGenerators;
    private readonly ILogger<GenerateEmbeddingsHandler> _log;

    /// <summary>
    /// Note: stepName and other params are injected with DI, <see cref="DependencyInjection.UseHandler{THandler}"/>
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="configuration">Application settings</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public GenerateEmbeddingsHandler(
        string stepName,
        SKMemoryConfig configuration,
        IPipelineOrchestrator orchestrator,
        ILogger<GenerateEmbeddingsHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<GenerateEmbeddingsHandler>.Instance;

        // Setup the active embedding generators config, strongly typed
        this._embeddingGenerators = configuration.GetHandlerConfig<EmbeddingGenerationConfig>(stepName, "EmbeddingGeneration").GetActiveGeneratorsTypedConfig(log);

        this._log.LogInformation("Handler ready: {0}", stepName);
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
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
                        // TODO: calc embeddings
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
