// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Microsoft.KernelMemory.DataFormats.Pdf;

public class PdfDecoder
{
    public List<DocumentPage> DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public List<DocumentPage> DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public List<DocumentPage> DocToText(Stream data)
    {
        var result = new List<DocumentPage>();
        StringBuilder sb = new();
        using var pdfDocument = PdfDocument.Open(data);
        foreach (Page? page in pdfDocument.GetPages())
        {
            string? text = ContentOrderTextExtractor.GetText(page);
            result.Add(new DocumentPage(text, page.Number));
        }

        return result;
    }
}
