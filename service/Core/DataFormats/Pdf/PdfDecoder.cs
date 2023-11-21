// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Microsoft.KernelMemory.DataFormats.Pdf;

public class PdfDecoder
{
    public string DocToText(string filename)
    {
        using var stream = File.OpenRead(filename);
        return this.DocToText(stream);
    }

    public string DocToText(BinaryData data)
    {
        using var stream = data.ToStream();
        return this.DocToText(stream);
    }

    public string DocToText(Stream data)
    {
        StringBuilder sb = new();
        using var pdfDocument = PdfDocument.Open(data);
        foreach (var page in pdfDocument.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            sb.Append(text);
        }

        return sb.ToString().Trim();
    }
}
