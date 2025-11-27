namespace KernelMemory.Core.Storage.Models;

/// <summary>
/// Request model for upserting content.
/// If ID is empty, a new one will be generated.
/// If ID is provided, the record will be replaced if it exists, or created if it doesn't.
/// </summary>
public class UpsertRequest
{
    /// <summary>
    /// Content ID. If empty, a new one will be generated.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The actual string content to store.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the content.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Optional content creation date. If not provided, current time is used.
    /// </summary>
    public DateTimeOffset? ContentCreatedAt { get; set; }

    /// <summary>
    /// Optional title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional tags.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Optional metadata key-value pairs.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
