// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// AST node representing a field reference in the query.
/// Supports dot notation for metadata access: metadata.author, metadata.project.name
/// </summary>
public sealed class FieldNode : QueryNode
{
    /// <summary>
    /// The full field path (e.g., "content", "metadata.author", "metadata.project.name").
    /// Case-insensitive (normalized to lowercase during parsing).
    /// </summary>
    public required string FieldPath { get; init; }

    /// <summary>
    /// Parsed field path segments for metadata access.
    /// Example: "metadata.author" → ["metadata", "author"]
    /// Example: "content" → ["content"]
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public string[] PathSegments => this.FieldPath.Split('.');

    /// <summary>
    /// True if this is a metadata field (starts with "metadata.").
    /// </summary>
    public bool IsMetadataField => this.FieldPath.StartsWith("metadata.", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Get the metadata key for metadata fields.
    /// Example: "metadata.author" → "author"
    /// Example: "metadata.project.name" → "project.name"
    /// Returns null for non-metadata fields.
    /// </summary>
    public string? MetadataKey
    {
        get
        {
            if (!this.IsMetadataField)
            {
                return null;
            }

            // Remove "metadata." prefix
            const string Prefix = "metadata.";
            return this.FieldPath.Substring(Prefix.Length);
        }
    }

    /// <summary>
    /// Accept a visitor for AST traversal.
    /// </summary>
    public override T Accept<T>(IQueryNodeVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}
