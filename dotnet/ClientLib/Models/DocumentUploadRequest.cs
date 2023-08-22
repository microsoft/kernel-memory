// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.SemanticMemory;

// Note: this class is designed to avoid using Asp.Net IForm
// and avoiding dependencies on Asp.Net HTTP that would lead
// to dependency issues mixing .NET7 and .NET Standard 2.0
public class DocumentUploadRequest
{
    public class UploadedFile
    {
        public string FileName { get; set; } = string.Empty;
        public Stream FileContent { get; set; } = Stream.Null;

        public UploadedFile()
        {
        }

        public UploadedFile(string fileName, Stream fileContent)
        {
            this.FileName = fileName;
            this.FileContent = fileContent;
        }
    }

    public string Index { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public TagCollection Tags { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public List<UploadedFile> Files { get; set; } = new List<UploadedFile>();
}
