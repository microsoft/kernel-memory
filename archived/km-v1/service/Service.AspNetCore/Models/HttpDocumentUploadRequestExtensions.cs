// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Http;

namespace Microsoft.KernelMemory.Service.AspNetCore.Models;

// Note: this class is designed to avoid using Asp.Net IForm
// and avoiding dependencies on Asp.Net HTTP that would lead
// to dependency issues mixing .NET7 and .NET Standard 2.0
public static class HttpDocumentUploadRequestExtensions
{
    public static DocumentUploadRequest ToDocumentUploadRequest(this HttpDocumentUploadRequest request)
    {
        var result = new DocumentUploadRequest
        {
            Index = request.Index,
            DocumentId = request.DocumentId,
            Tags = request.Tags,
            Steps = request.Steps
        };

        foreach (IFormFile file in request.Files)
        {
            result.Files.Add(new DocumentUploadRequest.UploadedFile
            {
                FileName = file.FileName,
                FileContent = file.OpenReadStream()
            });
        }

        return result;
    }
}
