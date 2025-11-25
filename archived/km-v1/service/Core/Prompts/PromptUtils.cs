// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.KernelMemory.Prompts;

internal static class PromptUtils
{
    private static readonly Regex s_tagsRegex = new(@"\{\{\$tags\[(.*?)\]\}\}");
    private static readonly Regex s_metadataRegex = new(@"\{\{\$meta\[(.*?)\]\}\}");

    // Note: the function doesn't cover scenarios where tags/metadata string replacements introduce extra unexpected placeholders.
    public static string RenderFactTemplate(
        string template,
        string factContent,
        string? source = "",
        string? relevance = "",
        string? recordId = "",
        TagCollection? tags = null,
        Dictionary<string, object>? metadata = null)
    {
        var result = template
            .Replace("{{$source}}", source, StringComparison.Ordinal)
            .Replace("{{$relevance}}", relevance, StringComparison.Ordinal)
            .Replace("{{$memoryId}}", recordId, StringComparison.Ordinal);

        // {{$tag[X]}}
        while (s_tagsRegex.IsMatch(result))
        {
            result = s_tagsRegex.Replace(result, match =>
            {
                string tagName = match.Groups[1].Value;
                if (tags == null || !tags.TryGetValue(tagName, out List<string?>? tagValues))
                {
                    return "-";
                }

                return tagValues.Count switch
                {
                    1 => tagValues[0]!,
                    > 1 => "[" + string.Join(", ", tagValues) + "]",
                    _ => "-"
                };
            });
        }

        // {{$tags}}
        result = result.Replace("{{$tags}}", tags != null ? tags.ToStringExcludeReserved() : string.Empty, StringComparison.Ordinal);

        // {{$meta[X]}}
        while (s_metadataRegex.IsMatch(result))
        {
            result = s_metadataRegex.Replace(result, match =>
            {
                if (metadata != null)
                {
                    string metadataKey = match.Groups[1].Value;
                    return metadata.TryGetValue(metadataKey, out object? metadataValue) ? $"{metadataValue}" : "-";
                }

                return "-";
            });
        }

        return result.Replace("{{$content}}", factContent, StringComparison.Ordinal);
    }
}
