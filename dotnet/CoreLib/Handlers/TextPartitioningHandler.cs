// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Text;
using Microsoft.SemanticMemory.Core.AppBuilders;
using Microsoft.SemanticMemory.Core.Diagnostics;
using Microsoft.SemanticMemory.Core.Pipeline;

namespace Microsoft.SemanticMemory.Core.Handlers;

public class TextPartitioningHandler : IPipelineStepHandler
{
    private const int OverlappingTokens = 30;
    private const int TokensPerLine = 500;
    private const int TokensPerParagraph = 1000;

    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<TextPartitioningHandler> _log;

    /// <summary>
    /// Note: stepName and other params are injected with DI, <see cref="DependencyInjection.UseHandler{THandler}"/>
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="log">Application logger</param>
    public TextPartitioningHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        ILogger<TextPartitioningHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._log = log ?? DefaultLogger<TextPartitioningHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    public string StepName { get; }

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken)
    {
        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = new();

            foreach (KeyValuePair<string, DataPipeline.GeneratedFileDetails> generatedFile in uploadedFile.GeneratedFiles)
            {
                var file = generatedFile.Value;

                if (uploadedFile.IsAlreadyPartitioned())
                {
                    this._log.LogDebug("File {0} has already been partitioned", uploadedFile.Name);
                    continue;
                }

                // Use a different partitioning strategy depending on the file type
                List<string> paragraphs;
                List<string> lines;
                switch (file.Type)
                {
                    case MimeTypes.PlainText:
                    {
                        this._log.LogDebug("Partitioning text file {0}", file.Name);
                        string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                        lines = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: TokensPerLine);
                        paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: TokensPerParagraph, overlapTokens: OverlappingTokens);
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = await this._orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                        lines = TextChunker.SplitMarkDownLines(content, maxTokensPerLine: TokensPerLine);
                        paragraphs = TextChunker.SplitMarkdownParagraphs(lines, maxTokensPerParagraph: TokensPerParagraph, overlapTokens: OverlappingTokens);
                        break;
                    }

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

                    newFiles.Add(destFile, new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        Type = MimeTypes.PlainText,
                        IsPartition = true,
                        ContentSHA256 = CalculateSHA256(text),
                    });
                }
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
