// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.KernelMemory.Service.AspNetCore.Models;

// Note: use multiform part serialization
public class HttpDocumentUploadRequest
{
    /// <summary>
    /// Name of the index where to store the data uploaded.
    /// </summary>
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// ID of the document.
    /// Note: the document might contain multiple files, which are identified by filename instead.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Optional tags to apply to the memories extracted from the document.
    /// Tags allow to filter memory records when searching and asking questions.
    /// </summary>
    public TagCollection Tags { get; set; } = [];

    /// <summary>
    /// Pipeline steps to execute, aka handlers uses to process the data uploaded.
    ///
    /// By default, KM processes files extracting text ('extract' step), chunking ('partition' step),
    /// calculating embeddings ('gen_embeddings' step), and storing records ('save_records').
    /// - The 'extract' step by default maps to TextExtractionHandler
    /// - The 'partition' step by default maps to TextPartitioningHandler
    /// - The 'gen_embeddings' step by default maps to GenerateEmbeddingsHandler
    /// - The 'save_records' step by default maps to SaveRecordsHandler
    /// The solution contains other handlers like SummarizationHandler
    ///
    /// These steps can be changed and customized, using custom handlers, implementing bespoke flows.
    /// For example, you can create handlers to zip files, send emails, write to DBs, etc.
    /// </summary>
    public List<string> Steps { get; set; } = [];

    /// <summary>
    /// Files uploaded
    /// </summary>
    public IEnumerable<IFormFile> Files { get; set; } = [];

    /// <summary>
    /// Optional custom arguments passed to handlers and other internal components.
    /// This collection can be used to pass custom data, to override default behavior, etc.
    /// </summary>
    public IDictionary<string, object?> ContextArguments { get; set; } = new Dictionary<string, object?>();

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
        if (form.TryGetValue(Constants.WebService.IndexField, out StringValues indexes) && indexes.Count > 1)
        {
            return (result, false, $"Invalid index name, '{Constants.WebService.IndexField}', multiple values provided");
        }

        // Only one document ID can be defined
        if (form.TryGetValue(Constants.WebService.DocumentIdField, out StringValues documentIds) && documentIds.Count > 1)
        {
            return (result, false, $"Invalid document ID, '{Constants.WebService.DocumentIdField}' must be a single value, not a list");
        }

        // Document ID is optional, e.g. used if the client wants to retry the same upload, otherwise a random/unique one is generated
        string? documentId = documentIds.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(documentId))
        {
            documentId = DateTimeOffset.Now.ToString("yyyyMMdd.HHmmss.", CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N");
        }

        // Optional document tags. Tags are passed in as "key:value", where a key can have multiple values. See TagCollection.
        if (form.TryGetValue(Constants.WebService.TagsField, out StringValues tags))
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
        if (form.TryGetValue(Constants.WebService.StepsField, out StringValues steps))
        {
            foreach (string? step in steps)
            {
                if (string.IsNullOrWhiteSpace(step)) { continue; }

                // Allow step names to be separated by space, comma, semicolon
                var list = step.Replace(' ', ';').Replace(',', ';').Split(';');
                result.Steps.AddRange(from s in list where !string.IsNullOrWhiteSpace(s) select s.Trim());
            }
        }

        // Optional key-value arguments, JSON encoded
        if (form.TryGetValue(Constants.WebService.ArgsField, out StringValues args))
        {
            if (args.Count > 1)
            {
                return (result, false, $"Invalid arguments, '{Constants.WebService.ArgsField}' must be a single JSON string value, not a list");
            }

            string? json = args.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                    if (arguments != null)
                    {
                        foreach (var arg in arguments)
                        {
                            result.ContextArguments[arg.Key] = arg.Value!;
                        }
                    }
                }
                catch (Exception e)
                {
                    return (result, false, $"Invalid arguments: '{e.Message}'. '{Constants.WebService.ArgsField}' must be a valid JSON string.");
                }
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
