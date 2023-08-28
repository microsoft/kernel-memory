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
    /// <summary>
    /// Note: this code is NOT .NET Standard 2.0 compatible, for which we have
    /// a similar version, see DocumentExtensions.ToDocumentUploadRequest()
    /// </summary>
    /// <param name="doc">Document to convert</param>
    /// <param name="index">Storage index</param>
    /// <param name="steps">Pipeline steps to execute</param>
    /// <returns>Instance of <see cref="DocumentUploadRequest"/>doc upload request</returns>
    /// <param name="cancellationToken">Async task cancellation token</param>
    /// <returns>Instance of <see cref="DocumentUploadRequest"/>doc upload request</returns>
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
        foreach (string fileName in doc.FileNames)
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

        foreach (KeyValuePair<string, Stream> stream in doc.Streams)
        {
            files.Add(new DocumentUploadRequest.UploadedFile(stream.Key, stream.Value));
        }

        uploadRequest.Files = files;

        return uploadRequest;
    }
}
