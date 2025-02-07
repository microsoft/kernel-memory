// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Prompts;
using Microsoft.KernelMemory.Text;

namespace Microsoft.KernelMemory.Handlers;

public sealed class SummarizationHandler : IPipelineStepHandler
{
    private const int MinLength = 30;

    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<SummarizationHandler> _log;
    private readonly string _summarizationPrompt;
    private readonly PlainTextChunker _plainTextChunker;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for generating a summary of each file in a document.
    /// The summary serves as an additional partition, aka it's part of the synthetic
    /// data generated for documents, in order to increase hit ratio and Q/A quality.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="promptProvider">Class responsible for providing a given prompt</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public SummarizationHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IPromptProvider? promptProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._plainTextChunker = new PlainTextChunker();

        promptProvider ??= new EmbeddedPromptProvider();
        this._summarizationPrompt = promptProvider.ReadPrompt(Constants.PromptNamesSummarize);

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<SummarizationHandler>();

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Generating summary, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> summaryFiles = [];

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var file = generatedFile.Value;

                if (file.AlreadyProcessedBy(this))
                {
                    this._log.LogTrace("File {0} already processed by this handler", file.Name);
                    continue;
                }

                // Summarize only the original content
                if (file.ArtifactType != DataPipeline.ArtifactTypes.ExtractedText)
                {
                    this._log.LogTrace("Skipping file {0}", file.Name);
                    continue;
                }

                switch (file.MimeType)
                {
                    case MimeTypes.PlainText:
                    case MimeTypes.MarkDown:
                        this._log.LogDebug("Summarizing text file {0}", file.Name);
                        string content = (await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false)).ToString();
                        (string summary, bool success) = await this.SummarizeAsync(content, pipeline.GetContext()).ConfigureAwait(false);
                        if (success)
                        {
                            var summaryData = new BinaryData(summary);
                            var destFile = uploadedFile.GetHandlerOutputFileName(this);
                            await this._orchestrator.WriteFileAsync(pipeline, destFile, summaryData, cancellationToken).ConfigureAwait(false);

                            summaryFiles.Add(destFile, new DataPipeline.GeneratedFileDetails
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                ParentId = uploadedFile.Id,
                                Name = destFile,
                                Size = summary.Length,
                                MimeType = MimeTypes.PlainText,
                                ArtifactType = DataPipeline.ArtifactTypes.SyntheticData,
                                Tags = pipeline.Tags.Clone().AddSyntheticTag(Constants.TagsSyntheticSummary),
                                ContentSHA256 = summaryData.CalculateSHA256(),
                            });
                        }

                        break;

                    default:
                        this._log.LogWarning("File {0} cannot be summarized, type not supported", file.Name);
                        continue;
                }

                file.MarkProcessedBy(this);
            }

            // Add new files to pipeline status
            foreach (var file in summaryFiles)
            {
                file.Value.MarkProcessedBy(this);
                uploadedFile.GeneratedFiles.Add(file.Key, file.Value);
            }
        }

        return (ReturnType.Success, pipeline);
    }

    private async Task<(string summary, bool skip)> SummarizeAsync(string content, IContext context)
    {
        ITextGenerator textGenerator = this._orchestrator.GetTextGenerator();
        int contentLength = textGenerator.CountTokens(content);
        this._log.LogTrace("Size of the content to summarize: {0} tokens", contentLength);

        // If the content is less than 30 tokens don't do anything and move on.
        if (contentLength < MinLength)
        {
            this._log.LogWarning("Content is too short to summarize ({0} tokens), nothing to do", contentLength);
            return (content, true);
        }

        // By default, the goal is to summarize to 50% of the model capacity (or less)
        int targetSummarySize = textGenerator.MaxTokenTotal / 2;

        // Allow to override the target goal using context arguments
        var customTargetSummarySize = context.GetCustomSummaryTargetTokenSizeOrDefault(-1);
        if (customTargetSummarySize > 0)
        {
            if (customTargetSummarySize > textGenerator.MaxTokenTotal / 2)
            {
                throw new ArgumentOutOfRangeException(
                    $"Custom summary size is too large, the max value allowed is {textGenerator.MaxTokenTotal / 2} (50% of the model capacity)");
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(customTargetSummarySize, 15);
            targetSummarySize = customTargetSummarySize;
        }

        this._log.LogTrace("Target goal: summary max size <= {0} tokens", targetSummarySize);

        // By default, use 25% of the previous paragraph when summarizing a paragraph
        int maxTokensPerParagraph = textGenerator.MaxTokenTotal / 4;

        // By default, use 6.2% of the model capacity for overlapping tokens
        // Allow to override the number of overlapping tokens using context arguments
        var overlappingTokens = context.GetCustomSummaryOverlappingTokensOrDefault(textGenerator.MaxTokenTotal / 16);

        this._log.LogTrace("Overlap setting: {0} tokens", overlappingTokens);

        // Summarize at least once
        var done = false;

        var summarizationPrompt = context.GetCustomSummaryPromptOrDefault(this._summarizationPrompt);

        // If chunks overlap, we need to dedupe the content, e.g. run at least one summarization call on the entire content
        var overlapToRemove = overlappingTokens > 0;

        // Since the summary is meant to be shorter than the content, reserve 50% of the model
        // capacity for input and 50% for output (aka the summary to generate)
        int maxInputTokens = textGenerator.MaxTokenTotal / 2;

        // After the first run (after overlaps have been introduced), check if the summarization is causing the content to grow
        bool firstRun = overlapToRemove;
        int previousLength = contentLength;
        while (!done)
        {
            var chunks = new List<string>();

            // If the content fits into half the model capacity, use a single paragraph
            if (contentLength <= maxInputTokens)
            {
                overlapToRemove = false;
                chunks.Add(content);
            }
            else
            {
                chunks = this._plainTextChunker.Split(content, new PlainTextChunkerOptions { MaxTokensPerChunk = maxTokensPerParagraph, Overlap = overlappingTokens });
            }

            this._log.LogTrace("Paragraphs to summarize: {0}", chunks.Count);
            var newContent = new StringBuilder();
            for (int index = 0; index < chunks.Count; index++)
            {
                string paragraph = chunks[index];
                this._log.LogTrace("Summarizing paragraph {0}", index);

                var filledPrompt = summarizationPrompt.Replace("{{$input}}", paragraph, StringComparison.OrdinalIgnoreCase);
                await foreach (var token in textGenerator.GenerateTextAsync(filledPrompt, new TextGenerationOptions()).ConfigureAwait(false))
                {
                    newContent.Append(token);
                }

                newContent.AppendLineNix();
            }

            content = newContent.ToString();
            contentLength = textGenerator.CountTokens(content);

            // If the compression fails, stop, log an error, and save the content generated this far.
            if (!firstRun && contentLength >= previousLength)
            {
                this._log.LogError(
                    "Summarization stopped, the content is not getting shorter: {0} tokens => {1} tokens. The summary has been saved but is longer than requested.",
                    previousLength, contentLength);
                return (content, true);
            }

            this._log.LogTrace("Summary length: {0} => {1}", previousLength, contentLength);
            previousLength = contentLength;

            firstRun = false;
            done = !overlapToRemove && (contentLength <= targetSummarySize);
        }

        return (content, true);
    }
}
