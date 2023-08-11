﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.SemanticMemory.Client.Models;

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

    public string DocumentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public TagCollection Tags { get; set; } = new();
    public List<UploadedFile> Files { get; set; } = new List<UploadedFile>();
}
