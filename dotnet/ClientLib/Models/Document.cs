// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.SemanticMemory.Client.Models;

/// <summary>
/// A document is a collection of one or multiple files, with additional
/// metadata such as tags and ownership.
/// </summary>
public class Document
{
    public List<string> FileNames { get; set; } = new();

    public DocumentDetails Details { get; set; } = new();

    public Document() { }

    public Document(string fileName)
    {
        this.FileNames.Add(fileName);
    }

    public Document(List<string> fileNames)
    {
        this.FileNames.AddRange(fileNames);
    }

    public Document(string fileName, DocumentDetails details)
    {
        this.FileNames.Add(fileName);
        this.Details = details;
    }

    public Document(List<string> fileNames, DocumentDetails details)
    {
        this.FileNames.AddRange(fileNames);
        this.Details = details;
    }

    public Document(string[] fileNames, DocumentDetails details)
    {
        this.FileNames.AddRange(fileNames);
        this.Details = details;
    }
}
