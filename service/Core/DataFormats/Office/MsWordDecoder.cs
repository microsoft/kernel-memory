// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Microsoft.KernelMemory.DataFormats.Office;

public class MsWordDecoder
{
    public FileContent ExtractContent(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.ExtractContent(stream);
    }

    public FileContent ExtractContent(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.ExtractContent(stream);
    }

    public FileContent ExtractContent(Stream data)
    {
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

            return result;
        }
        finally
        {
            wordprocessingDocument.Dispose();
        }
    }
}
