// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for extracting text from files and saving it to document storage.
/// </summary>
public sealed class TextExtractionHandler : IPipelineStepHandler, IDisposable
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IEnumerable<IContentDecoder> _decoders;
    private readonly IWebScraper _webScraper;
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
    /// <param name="webScraper">Web scraper instance used to fetch web pages</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public TextExtractionHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IEnumerable<IContentDecoder> decoders,
        IWebScraper? webScraper = null,
        ILoggerFactory? loggerFactory = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._decoders = decoders;
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<TextExtractionHandler>();
        this._webScraper = webScraper ?? new WebScraper();

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
            FileContent content = new(MimeTypes.PlainText);
            bool skipFile = false;

            if (fileContent.ToArray().Length > 0)
            {
                if (uploadedFile.MimeType == MimeTypes.WebPageUrl)
                {
                    var (downloadedPage, pageContent, skip) = await this.DownloadContentAsync(uploadedFile, fileContent, cancellationToken).ConfigureAwait(false);
                    skipFile = skip;
                    if (!skipFile)
                    {
                        (text, content, skipFile) = await this.ExtractTextAsync(downloadedPage, pageContent, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    (text, content, skipFile) = await this.ExtractTextAsync(uploadedFile, fileContent, cancellationToken).ConfigureAwait(false);
                }
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
                    MimeType = content.MimeType,
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
                    MimeType = content.MimeType,
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

    public void Dispose()
    {
        if (this._webScraper is not IDisposable x) { return; }

        x.Dispose();
    }

    private async Task<(DataPipeline.FileDetails downloadedPage, BinaryData pageContent, bool skip)> DownloadContentAsync(
        DataPipeline.FileDetails uploadedFile, BinaryData fileContent, CancellationToken cancellationToken)
    {
        var url = fileContent.ToString();
        this._log.LogDebug("Downloading web page specified in '{0}' and extracting text from '{1}'", uploadedFile.Name, url);
        if (string.IsNullOrWhiteSpace(url))
        {
            uploadedFile.Log(this, "The web page URL is empty");
            this._log.LogWarning("The web page URL is empty");
            return (uploadedFile, fileContent, skip: true);
        }

        var urlDownloadResult = await this._webScraper.GetContentAsync(url, cancellationToken).ConfigureAwait(false);
        if (!urlDownloadResult.Success)
        {
            uploadedFile.Log(this, $"Web page download error: {urlDownloadResult.Error}");
            this._log.LogWarning("Web page download error: {0}", urlDownloadResult.Error);
            return (uploadedFile, fileContent, skip: true);
        }

        if (urlDownloadResult.Content.Length == 0)
        {
            uploadedFile.Log(this, "The web page has no text content, skipping it");
            this._log.LogWarning("The web page has no text content, skipping it");
            return (uploadedFile, fileContent, skip: true);
        }

        // IMPORTANT: copy by value to avoid editing the source var
        DataPipeline.FileDetails? result = JsonSerializer.Deserialize<DataPipeline.FileDetails>(JsonSerializer.Serialize(uploadedFile));
        ArgumentNullExceptionEx.ThrowIfNull(result, nameof(result), "File details cloning failure");

        result.MimeType = urlDownloadResult.ContentType;
        result.Size = urlDownloadResult.Content.Length;

        return (result, urlDownloadResult.Content, skip: false);
    }

    private async Task<(string text, FileContent content, bool skipFile)> ExtractTextAsync(
        DataPipeline.FileDetails uploadedFile,
        BinaryData fileContent,
        CancellationToken cancellationToken)
    {
        // Define default empty content
        var content = new FileContent(MimeTypes.PlainText);

        if (string.IsNullOrEmpty(uploadedFile.MimeType))
        {
            uploadedFile.Log(this, $"File MIME type is empty, ignoring the file {uploadedFile.Name}");
            this._log.LogWarning("Empty MIME type, file '{0}' will be ignored", uploadedFile.Name);
            return (text: string.Empty, content, skipFile: true);
        }

        // Checks if there is a decoder that supports the file MIME type. If multiple decoders support this type, it means that
        // the decoder has been redefined, so it takes the last one.
        var decoder = this._decoders.LastOrDefault(d => d.SupportsMimeType(uploadedFile.MimeType));
        if (decoder is not null)
        {
            this._log.LogDebug("Extracting text from file '{0}' mime type '{1}' using extractor '{2}'",
                uploadedFile.Name, uploadedFile.MimeType, decoder.GetType().FullName);
            content = await decoder.DecodeAsync(fileContent, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            uploadedFile.Log(this, $"File MIME type not supported: {uploadedFile.MimeType}. Ignoring the file {uploadedFile.Name}.");
            this._log.LogWarning("File MIME type not supported: {0} - ignoring the file {1}", uploadedFile.MimeType, uploadedFile.Name);
            return (text: string.Empty, content, skipFile: true);
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

        return (text, content, skipFile: false);
    }
}
