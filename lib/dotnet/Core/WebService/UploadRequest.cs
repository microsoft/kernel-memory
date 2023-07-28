// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.SemanticMemory.Core.WebService;

public class UploadRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public IEnumerable<string> CollectionIds { get; set; } = new List<string>();
    public IEnumerable<IFormFile> Files { get; set; } = new List<IFormFile>();

    /* Resources:
     * https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-7.0
     * https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-7.0#upload-large-files-with-streaming
     * https://stackoverflow.com/questions/71499435/how-do-i-do-file-upload-using-asp-net-core-6-minimal-api
     * https://stackoverflow.com/questions/57033535/multipartformdatacontent-add-stringcontent-is-adding-carraige-return-linefeed-to
     */
    public static async Task<(UploadRequest model, bool isValid, string errMsg)> BindHttpRequestAsync(HttpRequest httpRequest)
    {
        const string UserField = "user";
        const string CollectionsField = "collections";
        const string DocumentIdField = "documentId";

        var result = new UploadRequest();

        // Content format validation
        if (!httpRequest.HasFormContentType)
        {
            return (result, false, "Invalid content, multipart form data not found");
        }

        // Read form
        IFormCollection form = await httpRequest.ReadFormAsync().ConfigureAwait(false);

        // There must be at least one file
        if (form.Files.Count == 0)
        {
            return (result, false, "No file was uploaded");
        }

        // TODO: extract user ID from auth headers
        if (!form.TryGetValue(UserField, out StringValues userIds) || userIds.Count != 1 || string.IsNullOrEmpty(userIds[0]))
        {
            return (result, false, $"Invalid or missing user ID, '{UserField}' value empty or not found, or multiple values provided");
        }

        // At least one collection must be specified. Note: the pipeline might decide to ignore the specified collections,
        // i.e. custom pipelines can override/ignore this value, depending on the implementation chosen.
        if (!form.TryGetValue(CollectionsField, out StringValues collectionIds) || collectionIds.Count == 0 || collectionIds.Any(string.IsNullOrEmpty))
        {
            return (result, false, $"Invalid or missing collection ID, '{CollectionsField}' list is empty or contains empty values");
        }

        if (form.TryGetValue(DocumentIdField, out StringValues documentIds) && documentIds.Count > 1)
        {
            return (result, false, $"Invalid document ID, '{DocumentIdField}' must be a single value, not a list");
        }

        // Document Id is optional, e.g. used if the client wants to retry the same upload, otherwise we generate a random/unique one
        result.DocumentId = documentIds.FirstOrDefault() ?? DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.", CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");

        result.UserId = userIds[0]!;
        result.CollectionIds = collectionIds;
        result.Files = form.Files;

        return (result, true, string.Empty);
    }
}
