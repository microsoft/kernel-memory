using System.Text.Json.Serialization;

namespace KernelMemory.Core.Config.Enums;

/// <summary>
/// Defines the access level for a memory node
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NodeAccessLevels
{
    /// <summary>Full read/write access</summary>
    Full,

    /// <summary>Read-only access, no modifications allowed</summary>
    ReadOnly,

    /// <summary>Write-only access, typically for ingestion pipelines</summary>
    WriteOnly,

    /// <summary>Node is disabled and not accessible</summary>
    Disabled
}
