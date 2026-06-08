// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Storage.Models;

/// <summary>
/// Data transfer object for content records.
/// Clean representation without operational fields like Ready flag.
/// </summary>
public class ContentDto
{
    public string Id { get; set; } = string.Empty;
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
}
