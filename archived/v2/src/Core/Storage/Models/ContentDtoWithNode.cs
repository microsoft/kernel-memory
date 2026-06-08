// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Storage.Models;

/// <summary>
/// Content DTO with node information included.
/// Used by CLI commands to show which node the content came from.
/// </summary>
public class ContentDtoWithNode
{
    public string Id { get; set; } = string.Empty;
    public string Node { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public DateTimeOffset ContentCreatedAt { get; set; }
    public DateTimeOffset RecordCreatedAt { get; set; }
    public DateTimeOffset RecordUpdatedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] Tags { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a ContentDtoWithNode from a ContentDto and node ID.
    /// </summary>
    /// <param name="content">The content DTO to wrap.</param>
    /// <param name="nodeId">The node ID to include.</param>
    /// <returns>A new ContentDtoWithNode instance.</returns>
    public static ContentDtoWithNode FromContentDto(ContentDto content, string nodeId)
    {
        return new ContentDtoWithNode
        {
            Id = content.Id,
            Node = nodeId,
            Content = content.Content,
            MimeType = content.MimeType,
            ByteSize = content.ByteSize,
            ContentCreatedAt = content.ContentCreatedAt,
            RecordCreatedAt = content.RecordCreatedAt,
            RecordUpdatedAt = content.RecordUpdatedAt,
            Title = content.Title,
            Description = content.Description,
            Tags = content.Tags,
            Metadata = content.Metadata
        };
    }
}
