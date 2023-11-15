// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.KernelMemory.Handlers;

public class TextPartitioningHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly TextPartitioningOptions _options;
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
        TextPartitioningOptions? options = null,
        ILogger<TextPartitioningHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._options = options ?? new TextPartitioningOptions();
        this._log = log ?? DefaultLogger<TextPartitioningHandler>.Instance;
        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Partitioning text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

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
                        string content = (await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false)).ToString();
                        lines = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: this._options.MaxTokensPerLine);
                        paragraphs = TextChunker.SplitPlainTextParagraphs(
                            lines, maxTokensPerParagraph: this._options.MaxTokensPerParagraph, overlapTokens: this._options.OverlappingTokens);
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = (await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false)).ToString();
                        lines = TextChunker.SplitMarkDownLines(content, maxTokensPerLine: this._options.MaxTokensPerLine);
                        paragraphs = TextChunker.SplitMarkdownParagraphs(
                            lines, maxTokensPerParagraph: this._options.MaxTokensPerParagraph, overlapTokens: this._options.OverlappingTokens);
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
                    BinaryData textData = new(text);

                    int gpt3TokenCount = GPT3Tokenizer.Encode(text).Count;
                    this._log.LogDebug("Partition size: {0} tokens", gpt3TokenCount);

                    var destFile = uploadedFile.GetPartitionFileName(index);
                    await this._orchestrator.WriteFileAsync(pipeline, destFile, textData, cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = MimeTypes.PlainText,
                        ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
                        ContentSHA256 = textData.CalculateSHA256(),
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
}
