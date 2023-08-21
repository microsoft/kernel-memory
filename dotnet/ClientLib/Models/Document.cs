// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.SemanticMemory.Client.Models;

/// <summary>
/// A document is a collection of one or multiple files, with additional
/// metadata such as tags and ownership.
/// </summary>
public class Document
{
    public string Id
    {
        get { return this._id; }
        set { this._id = string.IsNullOrWhiteSpace(value) ? RandomId() : value; }
    }

    public TagCollection Tags { get; set; } = new();

    public List<string> FileNames { get; set; } = new();

    public Document(string? id = null, TagCollection? tags = null, IEnumerable<string>? fileNames = null)
    {
        if (id == null || string.IsNullOrWhiteSpace(id)) { id = RandomId(); }

        this.Id = id;

        if (tags != null) { this.Tags = tags; }

        if (fileNames != null) { this.FileNames.AddRange(fileNames); }
    }

    public Document AddTag(string name, string value)
    {
        this.Tags.Add(name, value);
        return this;
    }

    public Document AddFile(string fileName)
    {
        this.FileNames.Add(fileName);
        return this;
    }

    public Document AddFiles(IEnumerable<string>? fileNames)
    {
        if (fileNames != null) { this.FileNames.AddRange(fileNames); }

        return this;
    }

    public Document AddFiles(string[]? fileNames)
    {
        if (fileNames != null) { this.FileNames.AddRange(fileNames); }

        return this;
    }

    #region private

    private string _id = string.Empty;

    private static string RandomId()
    {
        const string LocalDateFormat = "yyyyMMddhhmmssfffffff";
        return Guid.NewGuid().ToString("N") + DateTimeOffset.Now.ToString(LocalDateFormat, CultureInfo.InvariantCulture);
    }

    #endregion
}

public static class DocumentExtensions
{
    // Note: this code is a .NET Standard 2.0 version of ToDocumentUploadRequestAsync()
    public static DocumentUploadRequest ToDocumentUploadRequest(
        this Document doc,
        string? index,
        IEnumerable<string>? steps)
    {
        var uploadRequest = new DocumentUploadRequest
        {
            Index = IndexExtensions.CleanName(index),
            DocumentId = doc.Id,
            Tags = doc.Tags,
            Steps = steps?.ToList() ?? new List<string>(),
        };

        var files = new List<DocumentUploadRequest.UploadedFile>();
        foreach (var fileName in doc.FileNames)
        {
            if (!File.Exists(fileName))
            {
                throw new SemanticMemoryException($"File not found: {fileName}");
            }

            byte[] bytes = File.ReadAllBytes(fileName);
            var data = new BinaryData(bytes);
            var formFile = new DocumentUploadRequest.UploadedFile(fileName, data.ToStream());
            files.Add(formFile);
        }

        uploadRequest.Files = files;

        return uploadRequest;
    }
}
