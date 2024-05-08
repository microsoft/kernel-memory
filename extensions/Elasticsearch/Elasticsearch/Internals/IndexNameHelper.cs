// Copyright (c) Free Mind Labs, Inc. All rights reserved.

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <inheritdoc />
public class IndexNameHelper
{
    /// <inheritdoc />
    public IndexNameHelper(ElasticsearchConfig config)
    {
        this.IndexPrefix = config.IndexPrefix ?? string.Empty;
    }

    /// <summary>
    /// The prefix to use for all index names.
    /// </summary>
    public string IndexPrefix { get; }

    /// <inheritdoc />
    public bool TryConvert(string indexName, out (string ActualIndexName, IEnumerable<string> Errors) result)
    {
        indexName = indexName ?? throw new ArgumentNullException(nameof(indexName));

        // Convert to lowercase and replace underscores with hyphens to
        // have a consistent behavior with other storage types supported by Kernel Memory. (see #18)
        // TODO: I am not sure why it's necessary... Should look into this...
#pragma warning disable CA1304 // Specify CultureInfo
        indexName = (this.IndexPrefix + indexName)
            .Replace("_", "-", StringComparison.Ordinal)
            .Trim()
            .ToLower();
#pragma warning restore CA1304 // Specify CultureInfo

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

    /// <inheritdoc />
    public string Convert(string indexName)
    {
        if (!this.TryConvert(indexName, out var result))
        {
            throw new InvalidIndexNameException(result);
        }

        return result.ActualIndexName;
    }
}
