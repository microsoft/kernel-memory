// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query.Ast;

namespace KernelMemory.Core.Search.Query.Parsers;

/// <summary>
/// Interface for query parsers.
/// Implementations: InfixQueryParser (SQL-like), MongoJsonQueryParser (MongoDB JSON).
/// Both parsers produce the same unified AST structure.
/// </summary>
public interface IQueryParser
{
    /// <summary>
    /// Parse a query string into an AST.
    /// </summary>
    /// <param name="query">The query string to parse.</param>
    /// <returns>The parsed AST root node.</returns>
    /// <exception cref="QuerySyntaxException">If the query is malformed.</exception>
    QueryNode Parse(string query);

    /// <summary>
    /// Validate a query without parsing (fast check).
    /// </summary>
    /// <param name="query">The query string to validate.</param>
    /// <returns>True if the query is syntactically valid.</returns>
    bool Validate(string query);
}
