// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.DataFormats;

public class FileSection
{
    /// <summary>
    /// Text page number/Audio segment number/Video scene number
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonPropertyName("number")]
    public int Number { get; }

    /// <summary>
    /// Whether the first/last sentence may continue from the previous/into
    /// the next section (e.g. like PDF docs).
    /// true: the first/last sentence do not cross over, the first doesn't
    ///       continue from the previous section, and the last sentence ends
    ///       where the section ends (e.g. Powerpoint, Excel).
    /// false: the first sentence may be a continuation from the previous section,
    ///        and the last sentence may continue into the next section.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("complete")]
    public bool SentencesAreComplete { get; }

    /// <summary>
    /// Page text content
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("content")]
    public string Content { get; }

    public FileSection(int number, string? content, bool sentencesAreComplete)
    {
        this.Number = number;
        this.SentencesAreComplete = sentencesAreComplete;
        this.Content = content ?? string.Empty;
    }
}
