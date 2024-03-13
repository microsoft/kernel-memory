// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Image;
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.DataFormats.WebPages;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.Handlers;

/// <summary>
/// Memory ingestion pipeline handler responsible for extracting text from files and saving it to content storage.
/// </summary>
public class TextExtractionHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly WebScraper _webScraper;
    private readonly IOcrEngine? _ocrEngine;
    private readonly ILogger<TextExtractionHandler> _log;

    /// <inheritdoc />
    public string StepName { get; }

    /// <summary>
    /// Handler responsible for extracting text from documents.
    /// Note: stepName and other params are injected with DI.
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="ocrEngine">The ocr engine to use for parsing image files</param>
    /// <param name="log">Application logger</param>
    public TextExtractionHandler(
        string stepName,
        IPipelineOrchestrator orchestrator,
        IOcrEngine? ocrEngine = null,
        ILogger<TextExtractionHandler>? log = null)
    {
        this.StepName = stepName;
        this._orchestrator = orchestrator;
        this._ocrEngine = ocrEngine;
        this._log = log ?? DefaultLogger<TextExtractionHandler>.Instance;
        this._webScraper = new WebScraper(this._log);

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

        switch (uploadedFile.MimeType)
        {
            case MimeTypes.PlainText:
                this._log.LogDebug("Extracting text from plain text file {0}", uploadedFile.Name);
                content.Sections.Add(new(1, fileContent.ToString().Trim(), true));
                break;

            case MimeTypes.MarkDown:
                this._log.LogDebug("Extracting text from MarkDown file {0}", uploadedFile.Name);
                content.Sections.Add(new(1, fileContent.ToString().Trim(), true));
                extractType = MimeTypes.MarkDown;
                break;

            case MimeTypes.Json:
                this._log.LogDebug("Extracting text from JSON file {0}", uploadedFile.Name);
                content.Sections.Add(new(1, fileContent.ToString().Trim(), true));
                break;

            case MimeTypes.MsWordX:
                this._log.LogDebug("Extracting text from MS Word file {0}", uploadedFile.Name);
                content = new MsWordDecoder().ExtractContent(fileContent);
                break;

            case MimeTypes.MsPowerPointX:
                this._log.LogDebug("Extracting text from MS PowerPoint file {0}", uploadedFile.Name);
                content = new MsPowerPointDecoder().ExtractContent(fileContent,
                    withSlideNumber: true,
                    withEndOfSlideMarker: false,
                    skipHiddenSlides: true);
                break;

            case MimeTypes.MsExcelX:
                this._log.LogDebug("Extracting text from MS Excel file {0}", uploadedFile.Name);
                content = new MsExcelDecoder().ExtractContent(fileContent);
                break;

            case MimeTypes.MsWord:
            case MimeTypes.MsPowerPoint:
            case MimeTypes.MsExcel:
                skipFile = true;
                uploadedFile.Log(
                    this,
                    "Office 97-2003 format not supported. It is recommended to migrate to the newer OpenXML format (docx, xlsx or pptx). Ignoring the file."
                );
                this._log.LogWarning("Office 97-2003 file MIME type not supported: {0} - ignoring the file", uploadedFile.MimeType);
                break;

            case MimeTypes.Pdf:
                this._log.LogDebug("Extracting text from PDF file {0}", uploadedFile.Name);
                content = new PdfDecoder().ExtractContent(fileContent);
                break;

            case MimeTypes.Html:
                this._log.LogDebug("Extracting text from HTML file {0}", uploadedFile.Name);
                content = new HtmlDecoder().ExtractContent(fileContent);
                break;

            case MimeTypes.WebPageUrl:
                var url = fileContent.ToString();
                this._log.LogDebug("Downloading web page specified in {0} and extracting text from {1}", uploadedFile.Name, url);
                if (string.IsNullOrWhiteSpace(url))
                {
                    skipFile = true;
                    uploadedFile.Log(this, "The web page URL is empty");
                    this._log.LogWarning("The web page URL is empty");
                    break;
                }

                var result = await this._webScraper.GetTextAsync(url).ConfigureAwait(false);
                if (!result.Success)
                {
                    skipFile = true;
                    uploadedFile.Log(this, $"Download error: {result.Error}");
                    this._log.LogWarning("Web page download error: {0}", result.Error);
                    break;
                }

                if (string.IsNullOrEmpty(result.Text))
                {
                    skipFile = true;
                    uploadedFile.Log(this, "The web page has no text content, skipping it");
                    this._log.LogWarning("The web page has no text content, skipping it");
                    break;
                }

                content.Sections.Add(new(1, result.Text.Trim(), true));
                this._log.LogDebug("Web page {0} downloaded, text length: {1}", url, result.Text.Length);
                break;

            case MimeTypes.ImageJpeg:
            case MimeTypes.ImagePng:
            case MimeTypes.ImageTiff:
                this._log.LogDebug("Extracting text from image file {0}", uploadedFile.Name);
                if (this._ocrEngine == null)
                {
                    throw new NotSupportedException($"Image extraction not configured: {uploadedFile.Name}");
                }

                var imageText = await new ImageDecoder().ImageToTextAsync(this._ocrEngine, fileContent, cancellationToken).ConfigureAwait(false);
                content.Sections.Add(new(1, imageText.Trim(), true));
                break;

            case "":
                skipFile = true;
                uploadedFile.Log(this, "File MIME type is empty, ignoring the file");
                this._log.LogWarning("Empty MIME type, the file will be ignored");
                break;

            default:
                skipFile = true;
                uploadedFile.Log(this, $"File MIME type not supported: {uploadedFile.MimeType}. Ignoring the file.");
                this._log.LogWarning("File MIME type not supported: {0} - ignoring the file", uploadedFile.MimeType);
                break;
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
