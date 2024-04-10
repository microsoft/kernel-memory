// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for extracting text from files and saving it to content storage.
/// </summary>
public class TextExtractionHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IEnumerable<IContentDecoder> _decoders;
    private readonly ILogger<TextExtractionHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for extracting text from documents.
    /// Note: stepName and other params are injected with DI.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="decoders">The list of content decoders for extracting content</param>
    /// <param name="log">Application logger</param>
    public TextExtractionHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IEnumerable<IContentDecoder> decoders,
        ILogger<TextExtractionHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._decoders = decoders;
        this._log = log ?? DefaultLogger<TextExtractionHandler>.Instance;

        this._log.LogInformation("Handler '{0}' ready", stepName);
    }

    /// <inheritdoc />
    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Extracting text, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            if (uploadedFile.AlreadyProcessedBy(this))
            {
                this._log.LogTrace("File {0} already processed by this handler", uploadedFile.Name);
                continue;
            }

            var sourceFile = uploadedFile.Name;
            var destFile = $"{uploadedFile.Name}.extract.txt";
            var destFile2 = $"{uploadedFile.Name}.extract.json";
            BinaryData fileContent = await this._orchestrator.ReadFileAsync(pipeline, sourceFile, cancellationToken).ConfigureAwait(false);

            string text = string.Empty;
            FileContent content = new();
            string extractType = MimeTypes.PlainText;
            bool skipFile = false;

            if (fileContent.ToArray().Length > 0)
            {
                (text, content, extractType, skipFile) = await this.ExtractTextAsync(uploadedFile, fileContent, cancellationToken).ConfigureAwait(false);
            }

            // If the handler cannot extract text, we move on. There might be other handlers in the pipeline
            // capable of doing so, and in any case if a document contains multiple docs, the pipeline will
            // not fail, only do its best to export as much data as possible. The user can inspect the pipeline
            // status to know if a file has been ignored.
            if (!skipFile)
            {
                // Text file
                this._log.LogDebug("Saving extracted text file {0}", destFile);
                await this._orchestrator.WriteFileAsync(pipeline, destFile, new BinaryData(text), cancellationToken).ConfigureAwait(false);
                var destFileDetails = new DataPipeline.GeneratedFileDetails
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ParentId = uploadedFile.Id,
                    Name = destFile,
                    Size = text.Length,
                    MimeType = extractType,
                    ArtifactType = DataPipeline.ArtifactTypes.ExtractedText,
                    Tags = pipeline.Tags,
                };
                destFileDetails.MarkProcessedBy(this);
                uploadedFile.GeneratedFiles.Add(destFile, destFileDetails);

                // Structured content (pages)
                this._log.LogDebug("Saving extracted content {0}", destFile2);
                await this._orchestrator.WriteFileAsync(pipeline, destFile2, new BinaryData(content), cancellationToken).ConfigureAwait(false);
                var destFile2Details = new DataPipeline.GeneratedFileDetails
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ParentId = uploadedFile.Id,
                    Name = destFile2,
                    Size = text.Length,
                    MimeType = extractType,
                    ArtifactType = DataPipeline.ArtifactTypes.ExtractedContent,
                    Tags = pipeline.Tags,
                };
                destFile2Details.MarkProcessedBy(this);
                uploadedFile.GeneratedFiles.Add(destFile2, destFile2Details);
            }

            uploadedFile.MarkProcessedBy(this);
        }

        return (true, pipeline);
    }

    private async Task<(string text, FileContent content, string extractType, bool skipFile)> ExtractTextAsync(
        DataPipeline.FileDetails uploadedFile,
        BinaryData fileContent,
        CancellationToken cancellationToken)
    {
        bool skipFile = false;
        var content = new FileContent();
        string extractType = MimeTypes.PlainText;

        if (string.IsNullOrEmpty(uploadedFile.MimeType))
        {
            skipFile = true;
            uploadedFile.Log(this, "File MIME type is empty, ignoring the file");
            this._log.LogWarning("Empty MIME type, the file will be ignored");
        }
        else
        {
            var decoder = this._decoders.LastOrDefault(d => d.SupportedMimeTypes.Contains(uploadedFile.MimeType));
            if (decoder is not null)
            {
                var textContent = await decoder.ExtractContentAsync(this.StepName, uploadedFile, fileContent, cancellationToken).ConfigureAwait(false);
                if (textContent is null)
                {
                    // If the decoder returns null, it means it could not extract text from the file, so the file must be skipped.
                    skipFile = true;
                }

                content = textContent ?? new FileContent();
            }
            else
            {
                skipFile = true;
                uploadedFile.Log(this, $"File MIME type not supported: {uploadedFile.MimeType}. Ignoring the file.");
                this._log.LogWarning("File MIME type not supported: {0} - ignoring the file", uploadedFile.MimeType);
            }
        }

        var textBuilder = new StringBuilder();
        foreach (var section in content.Sections)
        {
            var sectionContent = section.Content.Trim();
            if (string.IsNullOrEmpty(sectionContent)) { continue; }

            textBuilder.Append(sectionContent);

            // Add a clean page separation
            if (section.SentencesAreComplete)
            {
                textBuilder.AppendLine();
                textBuilder.AppendLine();
            }
        }

        var text = textBuilder.ToString().Trim();

        return (text, content, extractType, skipFile);
    }
}
