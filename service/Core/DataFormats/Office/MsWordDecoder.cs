// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsWordDecoder : IContentDecoder
{
    private readonly ILogger<MsWordDecoder> _log;

    public IEnumerable<string> SupportedMimeTypes { get; } = [MimeTypes.MsWordX, MimeTypes.MsWord];

    public MsWordDecoder(ILogger<MsWordDecoder>? log = null)
    {
        this._log = log ?? DefaultLogger<MsWordDecoder>.Instance;
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return this.ExtractContentAsync(handlerStepName, file, stream, cancellationToken = default);
    }

    public Task<FileContent?> ExtractContentAsync(string handlerStepName, DataPipeline.FileDetails file, Stream data, CancellationToken cancellationToken = default)
    {
        if (file.MimeType == MimeTypes.MsWord)
        {
            file.Log(
                handlerStepName,
                "Office 97-2003 format not supported. It is recommended to migrate to the newer OpenXML format (docx). Ignoring the file."
            );

            this._log.LogWarning("Office 97-2003 file MIME type not supported: {0} - ignoring the file {1}", file.MimeType, file.Name);
            return Task.FromResult<FileContent?>(null);
        }

        this._log.LogDebug("Extracting text from MS Word file {0}", file.Name);

        var result = new FileContent();

        var wordprocessingDocument = WordprocessingDocument.Open(data, false);
        try
        {
            StringBuilder sb = new();

            MainDocumentPart? mainPart = wordprocessingDocument.MainDocumentPart;
            if (mainPart is null)
            {
                throw new InvalidOperationException("The main document part is missing.");
            }

            Body? body = mainPart.Document.Body;
            if (body is null)
            {
                throw new InvalidOperationException("The document body is missing.");
            }

            int pageNumber = 1;
            IEnumerable<Paragraph>? paragraphs = body.Descendants<Paragraph>();
            if (paragraphs != null)
            {
                foreach (Paragraph p in paragraphs)
                {
                    // Note: this is just an attempt at counting pages, not 100% reliable
                    // see https://stackoverflow.com/questions/39992870/how-to-access-openxml-content-by-page-number
                    var lastRenderedPageBreak = p.GetFirstChild<Run>()?.GetFirstChild<LastRenderedPageBreak>();
                    if (lastRenderedPageBreak != null)
                    {
                        string pageContent = sb.ToString().Trim();
                        sb.Clear();
                        result.Sections.Add(new FileSection(pageNumber, pageContent, true));
                        pageNumber++;
                    }

                    sb.AppendLine(p.InnerText);
                }
            }

            var lastPageContent = sb.ToString().Trim();
            result.Sections.Add(new FileSection(pageNumber, lastPageContent, true));

            return Task.FromResult(result)!;
        }
        finally
        {
            wordprocessingDocument.Dispose();
        }
    }
}
