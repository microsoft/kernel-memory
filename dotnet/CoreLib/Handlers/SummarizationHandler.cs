// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Tokenizers.GPT3;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.Prompts;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.KernelMemory.Handlers;

public class SummarizationHandler : IPipelineStepHandler
{
    private const int MinLength = 50;

    // OpenAI ADA:
    // * v1 max tokens is 2046 (GPT-2/GPT-3 tokenizer)
    // * v2 is 8191 (cl100k_base tokenizer)
    private const int SummaryMaxTokens = 2040;

    private const int MaxTokensPerParagraph = SummaryMaxTokens / 2;
    private const int MaxTokensPerLine = 300;
    private const int OverlappingTokens = 200;

    private readonly IPipelineOrchestrator _orchestrator;
    private readonly string _prompt;
    private readonly ILogger<SummarizationHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for generating a summary of each file in a document.
    /// The summary serves as an additional partition, aka it's part of the synthetic
    /// data generated for documents, in order to increase hit ratio and Q/A quality.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public SummarizationHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IPromptSupplier promptSupplier,
        ILogger<SummarizationHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._prompt = promptSupplier.ReadPrompt("summarize");
        this._log = log ?? DefaultLogger<SummarizationHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Generating summary, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> summaryFiles = new();

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
                        (string summary, bool success) = await this.SummarizeAsync(content).ConfigureAwait(false);
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

        return (true, pipeline);
    }

    private async Task<(string summary, bool skip)> SummarizeAsync(string content)
    {
        int contentLength = GPT3Tokenizer.Encode(content).Count;
        if (contentLength < MinLength)
        {
            this._log.LogDebug("Content too short to summarize, {0} tokens", contentLength);
            return (content, false);
        }

        ITextGeneration textGenerator = this._orchestrator.GetTextGenerator();

        // Summarize at least once
        var done = false;

        // If paragraphs overlap, we need to dedupe the content, e.g. run at least one summarization call on the entire content
        var overlapToRemove = OverlappingTokens > 0;

        // After the first run (after overlaps have been introduced), check if the summarization is causing the content to grow
        bool firstRun = overlapToRemove;
        int previousLength = contentLength;
        while (!done)
        {
            var paragraphs = new List<string>();
            if (contentLength <= SummaryMaxTokens)
            {
                overlapToRemove = false;
                paragraphs.Add(content);
            }
            else
            {
                List<string> lines = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: MaxTokensPerLine);
                paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: MaxTokensPerParagraph, overlapTokens: OverlappingTokens);
            }

            this._log.LogTrace("Paragraphs to summarize: {0}", paragraphs.Count);
            var newContent = new StringBuilder();
            for (int index = 0; index < paragraphs.Count; index++)
            {
                string paragraph = paragraphs[index];
                this._log.LogTrace("Summarizing paragraph {0}", index);

                var filledPrompt = this._prompt.Replace("{{$input}}", paragraph, StringComparison.OrdinalIgnoreCase);
                await foreach (string token in textGenerator.GenerateTextAsync(filledPrompt, new TextGenerationOptions()))
                {
                    newContent.Append(token);
                }

                newContent.AppendLine();
            }

            content = newContent.ToString();
            contentLength = GPT3Tokenizer.Encode(content).Count;

            if (!firstRun && contentLength >= previousLength)
            {
                this._log.LogError("Summarization failed, the content is getting longer: {0} tokens => {1} tokens", previousLength, contentLength);
                return (content, false);
            }

            this._log.LogTrace("Summary length: {0} => {1}", previousLength, contentLength);
            previousLength = contentLength;

            firstRun = false;
            done = !overlapToRemove && (contentLength <= SummaryMaxTokens);
        }

        return (content, true);
    }
}
