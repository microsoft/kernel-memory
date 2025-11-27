using System.Text.Json;

namespace KernelMemory.Core.Storage.Entities;

/// <summary>
/// Entity representing a content record in the Content table.
/// Source of truth for all content in the system.
/// </summary>
public class ContentRecord
{
    // Mandatory fields
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public bool Ready { get; set; }
    public DateTimeOffset ContentCreatedAt { get; set; }
    public DateTimeOffset RecordCreatedAt { get; set; }
    public DateTimeOffset RecordUpdatedAt { get; set; }

    // Optional fields
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // JSON-backed fields (stored as JSON strings in SQLite)
    // Tags array
    public string TagsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the tags array. Not mapped to database - uses TagsJson for persistence.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Tags
    {
        get => string.IsNullOrWhiteSpace(this.TagsJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(this.TagsJson) ?? [];
        set => this.TagsJson = JsonSerializer.Serialize(value);
    }

    // Metadata key-value pairs
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the metadata dictionary. Not mapped to database - uses MetadataJson for persistence.
    /// </summary>
    public Dictionary<string, string> Metadata
    {
        get => string.IsNullOrWhiteSpace(this.MetadataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(this.MetadataJson) ?? new Dictionary<string, string>();
        set => this.MetadataJson = JsonSerializer.Serialize(value);
    }
}
