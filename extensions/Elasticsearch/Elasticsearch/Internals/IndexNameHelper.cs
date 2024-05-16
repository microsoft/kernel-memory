// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

internal static class IndexNameHelper
{
    /// <summary>
    /// Tries to convert the given index name to a valid Elasticsearch index name.
    /// </summary>
    public static bool TryConvert(string indexName, ElasticsearchConfig config, out (string ActualIndexName, IEnumerable<string> Errors) result)
    {
        indexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        var indexPrefix = config?.IndexPrefix ?? string.Empty;

        // Convert to lowercase and replace underscores with hyphens to
        // have a consistent behavior with other storage types supported by Kernel Memory. (see #18)
        indexName = (indexPrefix + indexName)
            .Replace("_", "-", StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();

        // Check for null or whitespace
        if (string.IsNullOrWhiteSpace(indexName))
        {
            result = ("default", Array.Empty<string>());
            return true;
        }

        var errors = new List<string>();

        // Check for invalid start characters
        if (indexName.StartsWith('-') || indexName.StartsWith('_'))
        {
            errors.Add("An index name cannot start with a hyphen (-) or underscore (_).");
        }

        // Check for invalid characters
        if (indexName.Any(x => !char.IsLetterOrDigit(x) && x != '-'))
        {
            errors.Add("An index name can only contain letters, digits, and hyphens (-).");
        }

        // Check for length (max 255 bytes)
        if (System.Text.Encoding.UTF8.GetByteCount(indexName) > 255)
        {
            errors.Add("An index name cannot be longer than 255 bytes.");
        }

        // Avoid names that are dot-only or dot and numbers
        if (indexName.All(c => c == '.' || char.IsDigit(c)))
        {
            errors.Add("Index name cannot be only dots or dots and numbers.");
        }

        if (errors.Count > 0)
        {
            result = (string.Empty, errors);
            return false;
        }

        result = (indexName, Array.Empty<string>());
        return true;
    }

    /// <summary>
    /// Converts the given index name to a valid Elasticsearch index name.
    /// Throws an exception if the index name is invalid.
    /// </summary>
    /// <exception cref="InvalidIndexNameException"></exception>
    public static string Convert(string indexName, ElasticsearchConfig config)
    {
        if (!TryConvert(indexName, config, out var result))
        {
            throw new InvalidIndexNameException(result);
        }

        return result.ActualIndexName;
    }
}
