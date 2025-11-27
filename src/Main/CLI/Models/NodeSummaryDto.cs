namespace KernelMemory.Main.CLI.Models;

/// <summary>
/// Summary information about a configured node.
/// </summary>
public class NodeSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Access { get; init; } = string.Empty;
    public string ContentIndex { get; init; } = string.Empty;
    public bool HasFileStorage { get; init; }
    public bool HasRepoStorage { get; init; }
    public int SearchIndexCount { get; init; }
}
