// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.DataFormats.Text;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public sealed class TextPartitioningHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly TextPartitioningOptions _options;
    private readonly ILogger<TextPartitioningHandler> _log;
    private readonly TextChunker.TokenCounter _tokenCounter;
    private readonly int _maxTokensPerPartition = int.MaxValue;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for partitioning text in small chunks.
    /// Note: stepName and other params are injected with DI.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="options">The customize text partitioning option</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public TextPartitioningHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        TextPartitioningOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;

        this._options = options ?? new TextPartitioningOptions();
        this._options.Validate();

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<TextPartitioningHandler>();
        this._log.LogInformation("Handler '{0}' ready", stepName);

        this._tokenCounter = DefaultGPTTokenizer.StaticCountTokens;
        if (orchestrator.EmbeddingGenerationEnabled)
        {
            foreach (var gen in orchestrator.GetEmbeddingGenerators())
            {
                // Use the last tokenizer (TODO: revisit)
                this._tokenCounter = s => gen.CountTokens(s);
                this._maxTokensPerPartition = Math.Min(gen.MaxTokens, this._maxTokensPerPartition);
            }

            if (this._options.MaxTokensPerParagraph > this._maxTokensPerPartition)
            {
                throw ParagraphsTooBigForEmbeddingsException(this._options.MaxTokensPerParagraph, this._maxTokensPerPartition, this._log);
            }
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Partitioning text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        if (pipeline.Files.Count == 0)
        {
            this._log.LogWarning("Pipeline '{0}/{1}': there are no files to process, moving to next pipeline step.", pipeline.Index, pipeline.DocumentId);
            return (true, pipeline);
        }

        var context = pipeline.GetContext();

        // Allow to override the paragraph size using context arguments
        var maxTokensPerParagraph = context.GetCustomPartitioningMaxTokensPerParagraphOrDefault(this._options.MaxTokensPerParagraph);
        if (maxTokensPerParagraph > this._maxTokensPerPartition)
        {
            throw ParagraphsTooBigForEmbeddingsException(maxTokensPerParagraph, this._maxTokensPerPartition, this._log);
        }

        // Allow to override the number of overlapping tokens using context arguments
        var overlappingTokens = Math.Max(0, context.GetCustomPartitioningOverlappingTokensOrDefault(this._options.OverlappingTokens));

        string? chunkHeader = context.GetCustomPartitioningChunkHeaderOrDefault(null);

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
                List<string> partitions;
                List<string> sentences;
                BinaryData partitionContent = await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                string partitionsMimeType = MimeTypes.PlainText;

                // Skip empty partitions. Also: partitionContent.ToString() throws an exception if there are no bytes.
                if (partitionContent.ToArray().Length == 0) { continue; }

                switch (file.MimeType)
                {
                    case MimeTypes.PlainText:
                    {
                        this._log.LogDebug("Partitioning text file {0}", file.Name);
                        string content = partitionContent.ToString();
                        sentences = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: this._options.MaxTokensPerLine, tokenCounter: this._tokenCounter);
                        partitions = TextChunker.SplitPlainTextParagraphs(
                            sentences, maxTokensPerParagraph: maxTokensPerParagraph, overlapTokens: overlappingTokens, tokenCounter: this._tokenCounter, chunkHeader: chunkHeader);
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = partitionContent.ToString();
                        partitionsMimeType = MimeTypes.MarkDown;
                        sentences = TextChunker.SplitMarkDownLines(content, maxTokensPerLine: this._options.MaxTokensPerLine, tokenCounter: this._tokenCounter);
                        partitions = TextChunker.SplitMarkdownParagraphs(
                            sentences, maxTokensPerParagraph: maxTokensPerParagraph, overlapTokens: overlappingTokens, tokenCounter: this._tokenCounter);
                        break;
                    }

                    // TODO: add virtual/injectable logic
                    // TODO: see https://learn.microsoft.com/en-us/windows/win32/search/-search-ifilter-about

                    default:
                        this._log.LogWarning("File {0} cannot be partitioned, type '{1}' not supported", file.Name, file.MimeType);
                        // Don't partition other files
                        continue;
                }

                if (partitions.Count == 0) { continue; }

                this._log.LogDebug("Saving {0} file partitions", partitions.Count);
                for (int partitionNumber = 0; partitionNumber < partitions.Count; partitionNumber++)
                {
                    // TODO: turn partitions in objects with more details, e.g. page number
                    string text = partitions[partitionNumber];
                    int sectionNumber = 0; // TODO: use this to store the page number (if any)
                    BinaryData textData = new(text);

                    int tokenCount = this._tokenCounter(text);
                    this._log.LogDebug("Partition size: {0} tokens", tokenCount);

                    var destFile = uploadedFile.GetPartitionFileName(partitionNumber);
                    await this._orchestrator.WriteFileAsync(pipeline, destFile, textData, cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = partitionsMimeType,
                        ArtifactType = DataPipeline.ArtifactTypes.TextPartition,
                        PartitionNumber = partitionNumber,
                        SectionNumber = sectionNumber,
                        Tags = pipeline.Tags,
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

#pragma warning disable CA2254 // the msg is always used
    private static ConfigurationException ParagraphsTooBigForEmbeddingsException(int value, int limit, ILogger logger)
    {
        var errMsg = $"The configured partition size ({value} tokens) is too big for one " +
                     $"of the embedding generators in use. The max value allowed is {limit} tokens. " +
                     $"Consider changing the partitioning options, see {InternalConstants.DocsBaseUrl}/how-to/custom-partitioning for details.";
        logger.LogError(errMsg);
        return new ConfigurationException(errMsg);
    }
#pragma warning restore CA2254
}
