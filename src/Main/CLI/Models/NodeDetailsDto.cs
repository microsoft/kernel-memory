using System.Collections.Generic;

namespace KernelMemory.Main.CLI.Models;

/// <summary>
/// Detailed information about a node.
/// </summary>
public class NodeDetailsDto
{
    public string NodeId { get; init; } = string.Empty;
    public string Access { get; init; } = string.Empty;
    public ContentIndexConfigDto ContentIndex { get; init; } = new();
    public StorageConfigDto? FileStorage { get; init; }
    public StorageConfigDto? RepoStorage { get; init; }
    public List<SearchIndexDto> SearchIndexes { get; init; } = new();
}
