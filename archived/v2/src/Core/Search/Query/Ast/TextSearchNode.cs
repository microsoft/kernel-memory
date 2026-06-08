// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// AST node representing full-text search across FTS-indexed fields.
/// Used when no specific field is specified (default field behavior).
/// Searches across: title, description, content (all FTS-indexed fields).
/// Maps to: simple text query or MongoDB $text operator.
/// </summary>
public sealed class TextSearchNode : QueryNode
{
    /// <summary>
    /// The search text/pattern.
    /// Will be searched across all FTS-indexed fields (title, description, content).
    /// </summary>
    public required string SearchText { get; init; }

    /// <summary>
    /// Optional specific field to search in.
    /// If null, searches across all FTS-indexed fields (default behavior).
    /// If specified, searches only that field using FTS.
    /// </summary>
    public FieldNode? Field { get; init; }

    /// <summary>
    /// Accept a visitor for AST traversal.
    /// </summary>
    public override T Accept<T>(IQueryNodeVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}
