// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// Base class for all query AST nodes.
/// Abstract Syntax Tree (AST) representation of parsed queries.
/// Both infix and MongoDB JSON parsers produce this unified AST structure.
/// </summary>
public abstract class QueryNode
{
    /// <summary>
    /// Accept a visitor for traversal/transformation of the AST.
    /// Implements the Visitor pattern for extensibility.
    /// </summary>
    public abstract T Accept<T>(IQueryNodeVisitor<T> visitor);
}
