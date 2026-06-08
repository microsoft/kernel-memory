// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Models;

/// <summary>
/// Result of query validation (Q27 - dry-run mode).
/// Used to validate queries without executing them.
/// </summary>
public sealed class QueryValidationResult
{
    /// <summary>
    /// Whether the query is syntactically valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Detailed error message if invalid.
    /// Null if valid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Character position of error in query string.
    /// Null if valid or position cannot be determined.
    /// </summary>
    public int? ErrorPosition { get; init; }

    /// <summary>
    /// List of available/searchable fields.
    /// Useful for autocomplete and query building.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] AvailableFields { get; init; } = [];
}
