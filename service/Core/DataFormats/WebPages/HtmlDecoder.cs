// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using HtmlAgilityPack;

namespace Microsoft.KernelMemory.DataFormats.WebPages;

public class HtmlDecoder
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
        var doc = new HtmlDocument();
        doc.Load(data);

        var result = new FileContent();
        result.Sections.Add(new FileSection(1, doc.DocumentNode.InnerText.Trim(), true));
        return result;
    }
}
