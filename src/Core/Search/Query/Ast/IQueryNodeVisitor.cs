// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// Visitor interface for traversing the query AST.
/// Implements the Visitor pattern to decouple traversal logic from node structure.
/// Used for LINQ transformation, validation, and other AST operations.
/// </summary>
/// <typeparam name="T">Return type of the visitor methods.</typeparam>
public interface IQueryNodeVisitor<out T>
{
    /// <summary>Visit a logical node (AND, OR, NOT, NOR).</summary>
    T Visit(LogicalNode node);

    /// <summary>Visit a comparison node (==, !=, >=, etc.).</summary>
    T Visit(ComparisonNode node);

    /// <summary>Visit a text search node (FTS search).</summary>
    T Visit(TextSearchNode node);

    /// <summary>Visit a field reference node.</summary>
    T Visit(FieldNode node);

    /// <summary>Visit a literal value node.</summary>
    T Visit(LiteralNode node);
}
