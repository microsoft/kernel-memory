// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.KernelMemory.MemoryStorage;

/// <summary>
/// Extensions of <see cref="MemoryRecord"/>
/// </summary>
#pragma warning disable CA1055 // working with simple types
[Experimental("KMEXP00")]
public static class MemoryRecordExtensions
{
    /// <summary>
    /// Get document ID
    /// </summary>
    public static string GetDocumentId(this MemoryRecord record, ILogger? log = null)
    {
        return record.GetTagValue(Constants.ReservedDocumentIdTag, log);
    }

    /// <summary>
    /// Get file ID
    /// </summary>
    public static string GetFileId(this MemoryRecord record, ILogger? log = null)
    {
        return record.GetTagValue(Constants.ReservedFileIdTag, log);
    }

    /// <summary>
    /// Get partition number, starting from zero.
    /// </summary>
    public static int GetPartitionNumber(this MemoryRecord record, ILogger? log = null)
    {
        var value = record.GetTagValue(Constants.ReservedFilePartitionNumberTag, log);
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return int.TryParse(value, out int number) ? number : 0;
    }

    /// <summary>
    /// Get page number / audio segment number / video scene number
    /// </summary>
    public static int GetSectionNumber(this MemoryRecord record, ILogger? log = null)
    {
        var value = record.GetTagValue(Constants.ReservedFileSectionNumberTag, log);
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return int.TryParse(value, out int number) ? number : 0;
    }

    /// <summary>
    /// Get file MIME type
    /// </summary>
    public static string GetFileContentType(this MemoryRecord record, ILogger? log = null)
    {
        return record.GetTagValue(Constants.ReservedFileTypeTag, log);
    }

    /// <summary>
    /// Get web page URL, if the document was a web page
    /// </summary>
    public static string GetWebPageUrl(this MemoryRecord record, string indexName, ILogger? log = null)
    {
        var webPageUrl = record.GetPayloadValue(Constants.ReservedPayloadUrlField, log)?.ToString();

        if (!string.IsNullOrWhiteSpace(webPageUrl)) { return webPageUrl; }

        return Constants.HttpDownloadEndpointWithParams
            .Replace(Constants.HttpIndexPlaceholder, indexName, StringComparison.Ordinal)
            .Replace(Constants.HttpDocumentIdPlaceholder, record.GetDocumentId(), StringComparison.Ordinal)
            .Replace(Constants.HttpFilenamePlaceholder, record.GetFileName(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Get file name
    /// </summary>
    public static string GetFileName(this MemoryRecord record, ILogger? log = null)
    {
        return record.GetPayloadValue(Constants.ReservedPayloadFileNameField, log)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Get file name
    /// </summary>
    public static string GetPartitionText(this MemoryRecord record, ILogger? log = null)
    {
        return record.GetPayloadValue(Constants.ReservedPayloadTextField, log)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Get file name
    /// </summary>
    public static DateTimeOffset GetLastUpdate(this MemoryRecord record, ILogger? log = null)
    {
        var value = record.GetPayloadValue(Constants.ReservedPayloadLastUpdateField, log);
        return DateTimeOffset.TryParse(value?.ToString() ?? string.Empty, out var date) ? date : DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Return a memory record tag value if available
    /// </summary>
    public static string GetTagValue(this MemoryRecord record, string tagName, ILogger? log = null)
    {
        if (!record.Tags.TryGetValue(tagName, out List<string?>? tagValues))
        {
            log?.LogError("Memory record '{0}' doesn't contain a '{1}' tag", record.Id, tagName);
            return string.Empty;
        }

        return tagValues.FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Return a memory record tag value if available
    /// </summary>
    public static object? GetPayloadValue(this MemoryRecord record, string payloadKey, ILogger? log = null)
    {
        if (!record.Payload.TryGetValue(payloadKey, out object? value))
        {
            log?.LogError("Memory record '{0}' doesn't contain a '{1}' payload", record.Id, payloadKey);
            return null;
        }

        return value;
    }
}
#pragma warning restore CA1055
