// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// AST node representing logical operations (AND, OR, NOT, NOR).
/// Combines multiple query conditions with boolean logic.
/// </summary>
public sealed class LogicalNode : QueryNode
{
    /// <summary>
    /// The logical operator (AND, OR, NOT, NOR).
    /// </summary>
    public required LogicalOperator Operator { get; init; }

    /// <summary>
    /// Child conditions to combine.
    /// For NOT: single child (unary operator).
    /// For AND/OR/NOR: multiple children.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
    public required QueryNode[] Children { get; init; }

    /// <summary>
    /// Accept a visitor for AST traversal.
    /// </summary>
    public override T Accept<T>(IQueryNodeVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}
