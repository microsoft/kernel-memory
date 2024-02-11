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
    /// Whether a sentence can/cannot continue into the next page
    /// true: a sentence cannot cross pages.
    /// false: a sentence can continue into the next page.
    /// </summary>
    [JsonPropertyOrder(1)]
    [JsonPropertyName("pes")]
    public bool PagesEndSentences { get; }

    /// <summary>
    /// Page text content
    /// </summary>
    [JsonPropertyOrder(2)]
    [JsonPropertyName("content")]
    public string Content { get; }

    public FileSection(int number, string? content, bool pagesEndSentences)
    {
        this.Number = number;
        this.PagesEndSentences = pagesEndSentences;
        this.Content = content ?? string.Empty;
    }
}
