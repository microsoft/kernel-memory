// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.KernelMemory.Service.AspNetCore.Models;

// Note: use multiform part serialization
public class HttpDocumentUploadRequest
{
    public string Index { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public TagCollection Tags { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public IEnumerable<IFormFile> Files { get; set; } = new List<IFormFile>();

    /* Resources:
     * https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-7.0
     * https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-7.0#upload-large-files-with-streaming
     * https://stackoverflow.com/questions/71499435/how-do-i-do-file-upload-using-asp-net-core-6-minimal-api
     * https://stackoverflow.com/questions/57033535/multipartformdatacontent-add-stringcontent-is-adding-carraige-return-linefeed-to
     */
    public static async Task<(HttpDocumentUploadRequest model, bool isValid, string errMsg)> BindHttpRequestAsync(
        HttpRequest httpRequest, CancellationToken cancellationToken = default)
    {
        var result = new HttpDocumentUploadRequest();

        // Content format validation
        if (!httpRequest.HasFormContentType)
        {
            return (result, false, "Invalid content, multipart form data not found");
        }

        // Read form
        IFormCollection form = await httpRequest.ReadFormAsync(cancellationToken).ConfigureAwait(false);

        // There must be at least one file
        if (form.Files.Count == 0)
        {
            return (result, false, "No file was uploaded");
        }

        // Only one index can be defined
        if (form.TryGetValue(Constants.WebServiceIndexField, out StringValues indexes) && indexes.Count > 1)
        {
            return (result, false, $"Invalid index name, '{Constants.WebServiceIndexField}', multiple values provided");
        }

        // Only one document ID can be defined
        if (form.TryGetValue(Constants.WebServiceDocumentIdField, out StringValues documentIds) && documentIds.Count > 1)
        {
            return (result, false, $"Invalid document ID, '{Constants.WebServiceDocumentIdField}' must be a single value, not a list");
        }

        // Document Id is optional, e.g. used if the client wants to retry the same upload, otherwise a random/unique one is generated
        string? documentId = documentIds.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(documentId))
        {
            documentId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.", CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");
        }

        // Optional document tags. Tags are passed in as "key:value", where a key can have multiple values. See TagCollection.
        if (form.TryGetValue(Constants.WebServiceTagsField, out StringValues tags))
        {
            foreach (string? tag in tags)
            {
                if (tag == null) { continue; }

                var keyValue = tag.Trim().Split(Constants.ReservedEqualsChar, 2);
                string key = keyValue[0].Trim();
                if (string.IsNullOrWhiteSpace(key)) { continue; }

                ValidateTagName(key);

                string? value = keyValue.Length == 1 ? null : keyValue[1].Trim();
                if (string.IsNullOrWhiteSpace(value)) { value = null; }

                result.Tags.Add(key, value);
            }
        }

        // Optional pipeline steps. The user can pass a custom list or leave it to the system to use the default.
        if (form.TryGetValue(Constants.WebServiceStepsField, out StringValues steps))
        {
            foreach (string? step in steps)
            {
                if (string.IsNullOrWhiteSpace(step)) { continue; }

                // Allow step names to be separated by space, comma, semicolon
                var list = step.Replace(' ', ';').Replace(',', ';').Split(';');
                result.Steps.AddRange(from s in list where !string.IsNullOrWhiteSpace(s) select s.Trim());
            }
        }

        result.Index = indexes.FirstOrDefault()?.Trim() ?? string.Empty;
        result.DocumentId = documentId;
        result.Files = form.Files;

        return (result, true, string.Empty);
    }

    private static void ValidateTagName(string tagName)
    {
        if (tagName.StartsWith(Constants.ReservedTagsPrefix, StringComparison.Ordinal))
        {
            throw new KernelMemoryException(
                $"The tag name prefix '{Constants.ReservedTagsPrefix}' is reserved for internal use.");
        }

        if (tagName is Constants.ReservedDocumentIdTag
            or Constants.ReservedFileIdTag
            or Constants.ReservedFilePartitionTag
            or Constants.ReservedFileTypeTag
            or Constants.ReservedSyntheticTypeTag)
        {
            throw new KernelMemoryException($"The tag name '{tagName}' is reserved for internal use.");
        }
    }
}
