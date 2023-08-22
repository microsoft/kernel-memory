// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticMemory.WebService;

public static class DocumentExtensions
{
    // Note: this code is not .NET Standard 2.0 compatible
    public static async Task<DocumentUploadRequest> ToDocumentUploadRequestAsync(
        this Document doc,
        string? index,
        IEnumerable<string>? steps,
        CancellationToken cancellationToken)
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

            byte[] bytes = await File.ReadAllBytesAsync(fileName, cancellationToken).ConfigureAwait(false);
            var data = new BinaryData(bytes);
            var formFile = new DocumentUploadRequest.UploadedFile(fileName, data.ToStream());
            files.Add(formFile);
        }

        uploadRequest.Files = files;

        return uploadRequest;
    }
}
