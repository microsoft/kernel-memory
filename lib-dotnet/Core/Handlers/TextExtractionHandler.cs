﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.SemanticMemory.Core.Pipeline;
using Microsoft.SemanticKernel.Services.DataFormats.Office;
using Microsoft.SemanticKernel.Services.DataFormats.Pdf;

namespace Microsoft.SemanticKernel.SemanticMemory.Core.Handlers;

public class TextExtractionHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<TextExtractionHandler> _log;

    public TextExtractionHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<TextExtractionHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? NullLogger<TextExtractionHandler>.Instance;
    }

    /// <inheritdoc />
    public string StepName { get; }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken)
    {
        foreach (DataPipeline.FileDetails file in pipeline.Files)
        {
            var sourceFile = file.Name;
            var destFile = $"{file.Name}.extract.txt";
            BinaryData fileContent = await this._orchestrator.ReadFileAsync(pipeline, sourceFile, cancellationToken).ConfigureAwait(false);
            string text = string.Empty;

            switch (file.Type)
            {
                case MimeTypes.PlainText:
                    text = fileContent.ToString();
                    break;

                case MimeTypes.MsWord:
                    text = new MsWordDecoder().DocToText(fileContent);
                    break;

                case MimeTypes.Pdf:
                    text = new PdfDecoder().DocToText(fileContent);
                    break;

                default:
                    throw new NotSupportedException($"File type not supported: {file.Type}");
            }

            await this._orchestrator.WriteFileAsync(pipeline, destFile, BinaryData.FromString(text), cancellationToken).ConfigureAwait(false);

            file.GeneratedFiles.Add(destFile);
        }

        return (true, pipeline);
    }
}
