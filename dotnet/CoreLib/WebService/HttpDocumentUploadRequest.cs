// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.SemanticMemory.WebService;

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
        string indexField = Constants.WebServiceIndexField;
        string documentIdField = Constants.WebServiceDocumentIdField;
        string stepsField = Constants.WebServiceStepsField;

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

        if (form.TryGetValue(indexField, out StringValues indexes) && indexes.Count > 1)
        {
            return (result, false, $"Invalid index name, '{indexField}', multiple values provided");
        }

        if (form.TryGetValue(documentIdField, out StringValues documentIds) && documentIds.Count > 1)
        {
            return (result, false, $"Invalid document ID, '{documentIdField}' must be a single value, not a list");
        }

        // Document Id is optional, e.g. used if the client wants to retry the same upload, otherwise we generate a random/unique one
        var documentId = documentIds.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(documentId))
        {
            documentId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.", CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");
        }

        // Optional pipeline steps. The user can pass a custom list or leave it to the system to use the default.
        if (form.TryGetValue(stepsField, out StringValues steps))
        {
            foreach (string? step in steps)
            {
                if (string.IsNullOrWhiteSpace(step)) { continue; }

                var list = step.Replace(' ', ';').Replace(',', ';').Split(';');
                result.Steps.AddRange(from s in list where !string.IsNullOrWhiteSpace(s) select s.Trim());
            }
        }

        result.DocumentId = documentId;
        result.Index = indexes[0]!;
        result.Files = form.Files;

        // Store any extra field as a tag
        foreach (string key in form.Keys)
        {
            if (key == documentIdField
                || key == indexField
                || key == stepsField
                || !form.TryGetValue(key, out StringValues values)) { continue; }

            ValidateTagName(key);
            foreach (string? x in values)
            {
                result.Tags.Add(key, x);
            }
        }

        return (result, true, string.Empty);
    }

    private static void ValidateTagName(string key)
    {
        if (key.Contains('=', StringComparison.Ordinal))
        {
            throw new SemanticMemoryException("A tag name cannot contain the '=' symbol");
        }

        if (key is
            Constants.ReservedDocumentIdTag
            or Constants.ReservedFileIdTag
            or Constants.ReservedFilePartitionTag
            or Constants.ReservedFileTypeTag)
        {
            throw new SemanticMemoryException($"The tag name '{key}' is reserved for internal use.");
        }
    }
}
