﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using Microsoft.SemanticMemory.AI.Tokenizers.GPT3;
using Microsoft.SemanticMemory.Diagnostics;
using Microsoft.SemanticMemory.Pipeline;

namespace Microsoft.SemanticMemory.Handlers;

public class TextPartitioningHandler : IPipelineStepHandler
{
    private readonly int _maxTokensPerLine = 300;
    private readonly int _maxTokensPerParagraph = 1000;
    private readonly int _overlappingTokens = 100;

    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<TextPartitioningHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for partitioning text in small chunks.
    /// Note: stepName and other params are injected with DI.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="options">The customize text partitioning option</param>
    /// <param name="log">Application logger</param>
    public TextPartitioningHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        TextPartitioningOption? options = null,
        ILogger<TextPartitioningHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<TextPartitioningHandler>.Instance;

        if (options is not null)
        {
            this._maxTokensPerLine = options.MaxTokensPerLine;
            this._maxTokensPerParagraph = options.MaxTokensPerParagraph;
            this._overlappingTokens = options.OverlappingTokens;
        }

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var file = generatedFile.Value;
                if (file.AlreadyProcessedBy(this))
                {
                    this._log.LogTrace("File {0} already processed by this handler", file.Name);
                    continue;
                }

                // Partition only the original text
                if (file.ArtifactType != DataPipeline.ArtifactTypes.ExtractedText)
                {
                    this._log.LogTrace("Skipping file {0} (not original text)", file.Name);
                    continue;
                }

                // Use a different partitioning strategy depending on the file type
                List<string> paragraphs;
                List<string> lines;
                switch (file.MimeType)
                {
                    case MimeTypes.PlainText:
                    {
                        this._log.LogDebug("Partitioning text file {0}", file.Name);
                        string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                        lines = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: this._maxTokensPerLine);
                        paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: this._maxTokensPerParagraph, overlapTokens: this._overlappingTokens);
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                        lines = TextChunker.SplitMarkDownLines(content, maxTokensPerLine: this._maxTokensPerLine);
                        paragraphs = TextChunker.SplitMarkdownParagraphs(lines, maxTokensPerParagraph: this._maxTokensPerParagraph, overlapTokens: this._overlappingTokens);
                        break;
                    }

                    // TODO: add virtual/injectable logic
                    // TODO: see https://learn.microsoft.com/en-us/windows/win32/search/-search-ifilter-about

                    default:
                        this._log.LogWarning("File {0} cannot be partitioned, type not supported", file.Name);
                        // Don't partition other files
                        continue;
                }

                if (paragraphs.Count == 0) { continue; }

                this._log.LogDebug("Saving {0} file partitions", paragraphs.Count);
                for (int index = 0; index < paragraphs.Count; index++)
                {
                    string text = paragraphs[index];

                    int gpt3TokenCount = GPT3Tokenizer.Encode(text).Count;
                    this._log.LogDebug("Partition size: {0} tokens", gpt3TokenCount);

                    var destFile = uploadedFile.GetPartitionFileName(index);
                    await this._orchestrator.WriteTextFileAsync(pipeline, destFile, text, cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = MimeTypes.PlainText,
                        ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
                        ContentSHA256 = CalculateSHA256(text),
                    };
                    newFiles.Add(destFile, destFileDetails);
                    destFileDetails.MarkProcessedBy(this);
                }

                file.MarkProcessedBy(this);
            }

            // Add new files to pipeline status
            foreach (var file in newFiles)
            {
                uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        return (true, pipeline);
    }

    private static string CalculateSHA256(string value)
    {
        byte[] byteArray = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(byteArray).ToLowerInvariant();
    }
}
