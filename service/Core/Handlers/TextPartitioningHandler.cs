// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.Context;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Extensions;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

public sealed class TextPartitioningHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly TextPartitioningOptions _options;
    private readonly ILogger<TextPartitioningHandler> _log;
    private readonly int _maxTokensPerPartition = int.MaxValue;
    private readonly PlainTextChunker _plainTextChunker;
    private readonly MarkDownChunker _markDownChunker;

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
        this._plainTextChunker = new PlainTextChunker(new CL100KTokenizer());
        this._markDownChunker = new MarkDownChunker(new CL100KTokenizer());

        this._options = options ?? new TextPartitioningOptions();
        this._options.Validate();

        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<TextPartitioningHandler>();
        this._log.LogInformation("Handler '{0}' ready", stepName);

        if (orchestrator.EmbeddingGenerationEnabled)
        {
            // Use the last tokenizer (TODO: revisit)
            foreach (var gen in orchestrator.GetEmbeddingGenerators())
            {
                this._maxTokensPerPartition = Math.Min(gen.MaxTokens, this._maxTokensPerPartition);
            }

            if (this._options.MaxTokensPerParagraph > this._maxTokensPerPartition)
            {
                throw ChunkTooBigForEmbeddingsException(this._options.MaxTokensPerParagraph, this._maxTokensPerPartition, this._log);
            }
        }
    }

    /// <inheritdoc />
    public async Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Partitioning text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        if (pipeline.Files.Count == 0)
        {
            this._log.LogWarning("Pipeline '{0}/{1}': there are no files to process, moving to next pipeline step.", pipeline.Index, pipeline.DocumentId);
            return (ReturnType.Success, pipeline);
        }

        var context = pipeline.GetContext();

        // Allow to override the paragraph size using context arguments
        var maxTokensPerChunk = context.GetCustomPartitioningMaxTokensPerChunkOrDefault(this._options.MaxTokensPerParagraph);
        if (maxTokensPerChunk > this._maxTokensPerPartition)
        {
            throw ChunkTooBigForEmbeddingsException(maxTokensPerChunk, this._maxTokensPerPartition, this._log);
        }

        // Allow to override the number of overlapping tokens using context arguments
        var overlappingTokens = Math.Max(0, context.GetCustomPartitioningOverlappingTokensOrDefault(this._options.OverlappingTokens));

        string? chunkHeader = context.GetCustomPartitioningChunkHeaderOrDefault(null);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Track new files being generated (cannot edit originalFile.GeneratedFiles while looping it)
            Dictionary<string, DataPipeline.GeneratedFileDetails> newFiles = [];

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
                List<string> chunks;
                BinaryData fileContent = await this._orchestrator.ReadFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
                string chunksMimeType = MimeTypes.PlainText;

                // Skip empty partitions. Also: partitionContent.ToString() throws an exception if there are no bytes.
                if (fileContent.IsEmpty) { continue; }

                switch (file.MimeType)
                {
                    case MimeTypes.PlainText:
                    {
                        this._log.LogDebug("Partitioning text file {0}", file.Name);
                        string content = fileContent.ToString();
                        chunks = this._plainTextChunker.Split(content, new PlainTextChunkerOptions { MaxTokensPerChunk = maxTokensPerChunk, Overlap = overlappingTokens, ChunkHeader = chunkHeader });
                        break;
                    }

                    case MimeTypes.MarkDown:
                    {
                        this._log.LogDebug("Partitioning MarkDown file {0}", file.Name);
                        string content = fileContent.ToString();
                        chunksMimeType = MimeTypes.MarkDown;
                        chunks = this._markDownChunker.Split(content, new MarkDownChunkerOptions { MaxTokensPerChunk = maxTokensPerChunk, Overlap = overlappingTokens, ChunkHeader = chunkHeader });
                        break;
                    }

                    // TODO: add virtual/injectable logic
                    // TODO: see https://learn.microsoft.com/en-us/windows/win32/search/-search-ifilter-about

                    default:
                        this._log.LogWarning("File {0} cannot be partitioned, type '{1}' not supported", file.Name, file.MimeType);
                        // Don't partition other files
                        continue;
                }

                if (chunks.Count == 0) { continue; }

                this._log.LogDebug("Saving {0} file partitions", chunks.Count);
                for (int partitionNumber = 0; partitionNumber < chunks.Count; partitionNumber++)
                {
                    // TODO: turn partitions in objects with more details, e.g. page number
                    string text = chunks[partitionNumber];
                    int sectionNumber = 0; // TODO: use this to store the page number (if any)
                    BinaryData textData = new(text);

                    var destFile = uploadedFile.GetPartitionFileName(partitionNumber);
                    await this._orchestrator.WriteFileAsync(pipeline, destFile, textData, cancellationToken).ConfigureAwait(false);

                    var destFileDetails = new DataPipeline.GeneratedFileDetails
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ParentId = uploadedFile.Id,
                        Name = destFile,
                        Size = text.Length,
                        MimeType = chunksMimeType,
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

        return (ReturnType.Success, pipeline);
    }

#pragma warning disable CA2254 // the msg is always used
    private static ConfigurationException ChunkTooBigForEmbeddingsException(int value, int limit, ILogger logger)
    {
        var errMsg = $"The configured partition size ({value} tokens) is too big for one " +
                     $"of the embedding generators in use. The max value allowed is {limit} tokens. " +
                     $"Consider changing the partitioning options, see {InternalConstants.DocsBaseUrl}/how-to/custom-partitioning for details.";
        logger.LogError(errMsg);
        return new ConfigurationException(errMsg);
    }
#pragma warning restore CA2254
}
