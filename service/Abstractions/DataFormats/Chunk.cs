// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.DataFormats;

public class Chunk
{
    // Metadata keys
    private const string MetaSentencesAreComplete = "completeSentences";
    private const string MetaPageNumber = "pageNumber";

    /// <summary>
    /// Text page number/Audio segment number/Video scene number
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonPropertyName("number")]
    public int Number { get; }

    /// <summary>
    /// Page text content
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Optional metadata attached to the section.
    /// Values are JSON strings to be serialized/deserialized.
    /// Examples:
    /// - sentences are complete y/n
    /// - page number
    /// </summary>
    [JsonPropertyOrder(10)]
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    [JsonIgnore]
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Whether the first/last sentence may continue from the previous/into
    /// the next section (e.g. like PDF docs).
    /// true: the first/last sentence do not cross over, the first doesn't
    ///       continue from the previous section, and the last sentence ends
    ///       where the section ends (e.g. PowerPoint, Excel).
    /// false: the first sentence may be a continuation from the previous section,
    ///        and the last sentence may continue into the next section.
    /// </summary>
    [JsonIgnore]
    public bool SentencesAreComplete
    {
        get
        {
            return this.Metadata.TryGetValue(MetaSentencesAreComplete, out var value) && JsonSerializer.Deserialize<bool>(value);
        }
    }

    [JsonIgnore]
    public int PageNumber
    {
        get
        {
            if (this.Metadata.TryGetValue(MetaPageNumber, out var value))
            {
                return JsonSerializer.Deserialize<int>(value);
            }

            return -1;
        }
    }

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="number">Position within the parent content container</param>
    /// <param name="text">Text content</param>
    public Chunk(string? text, int number)
    {
        this.Content = text ?? string.Empty;
        this.Number = number;
        this.Metadata = new();
    }

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="number">Position within the parent content container</param>
    /// <param name="text">Text content</param>
    public Chunk(char text, int number)
    {
        this.Content = text.ToString();
        this.Number = number;
        this.Metadata = new();
    }

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="number">Position within the parent content container</param>
    /// <param name="text">Text content</param>
    public Chunk(StringBuilder text, int number)
    {
        this.Content = text.ToString();
        this.Number = number;
        this.Metadata = new();
    }

    /// <summary>
    /// Create new instance
    /// </summary>
    /// <param name="number">Position within the parent content container</param>
    /// <param name="text">Text content</param>
    /// <param name="metadata">Chunk metadata</param>
    public Chunk(string? text, int number, Dictionary<string, string> metadata)
    {
        this.Content = text ?? string.Empty;
        this.Number = number;
        this.Metadata = metadata;
    }

    /// <summary>
    /// Metadata builder
    /// </summary>
    /// <param name="sentencesAreComplete">Whether the first/last sentence may continue from the previous/into the next section</param>
    /// <param name="pageNumber">Number of the page where the content is extracted from</param>
    public static Dictionary<string, string> Meta(
        bool? sentencesAreComplete = null,
        int? pageNumber = null)
    {
        var result = new Dictionary<string, string>();

        if (sentencesAreComplete.HasValue)
        {
            result.Add(MetaSentencesAreComplete, JsonSerializer.Serialize(sentencesAreComplete.Value));
        }

        if (pageNumber.HasValue)
        {
            result.Add(MetaPageNumber, JsonSerializer.Serialize(pageNumber.Value));
        }

        return result;
    }
}
