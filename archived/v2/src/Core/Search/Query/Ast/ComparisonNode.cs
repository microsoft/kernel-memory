// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// AST node representing field comparison operations.
/// Examples: field==value, field>=date, field:~"pattern", tags:[AI,ML]
/// </summary>
public sealed class ComparisonNode : QueryNode
{
    /// <summary>
    /// The field being compared (e.g., "content", "metadata.author").
    /// Can be a simple field name or dot-notation path.
    /// </summary>
    public required FieldNode Field { get; init; }

    /// <summary>
    /// The comparison operator (==, !=, >=, etc.).
    /// </summary>
    public required ComparisonOperator Operator { get; init; }

    /// <summary>
    /// The value to compare against.
    /// Can be string, number, date, or array of values.
    /// Null for Exists operator (checking field presence).
    /// </summary>
    public LiteralNode? Value { get; init; }

    /// <summary>
    /// Accept a visitor for AST traversal.
    /// </summary>
    public override T Accept<T>(IQueryNodeVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}
