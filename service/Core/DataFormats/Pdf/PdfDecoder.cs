// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Microsoft.KernelMemory.DataFormats.Pdf;

public class PdfDecoder
{
    public List<FileSection> DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public List<FileSection> DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public List<FileSection> DocToText(Stream data)
    {
        var result = new List<FileSection>();
        using PdfDocument? pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return result; }

        foreach (Page? page in pdfDocument.GetPages().Where(x => x != null))
        {
            // Note: no trimming, use original spacing
            string pageContent = (ContentOrderTextExtractor.GetText(page) ?? string.Empty);
            result.Add(new FileSection(page.Number, pageContent, false));
        }

        return result;
    }
}
