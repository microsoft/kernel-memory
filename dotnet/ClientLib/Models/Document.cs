// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticMemory.Client.Models;

public class Document
{
    public string FileName { get; set; } = string.Empty;

    public DocumentDetails Details { get; set; } = new();

    public Document() { }

    public Document(string fileName) { this.FileName = fileName; }

    public Document(string fileName, DocumentDetails details)
    {
        this.FileName = fileName;
        this.Details = details;
    }
}
