// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Core.Search.Query.Ast;

namespace KernelMemory.Core.Search.Query.Parsers;

/// <summary>
/// Factory for creating query parsers.
/// Auto-detects query format (JSON vs infix) and returns appropriate parser.
/// </summary>
public static class QueryParserFactory
{
    /// <summary>
    /// Parse a query string using auto-detected format.
    /// Detection rule: starts with '{' = JSON, otherwise = infix.
    /// </summary>
    /// <param name="query">The query string to parse.</param>
    /// <returns>The parsed AST root node.</returns>
    /// <exception cref="QuerySyntaxException">If the query is malformed.</exception>
    public static QueryNode Parse(string query)
    {
        IQueryParser parser = DetectFormat(query);
        return parser.Parse(query);
    }

    /// <summary>
    /// Detect query format and return appropriate parser.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <returns>The appropriate parser for the detected format.</returns>
    public static IQueryParser DetectFormat(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty", nameof(query));
        }

        // Trim whitespace for detection
        string trimmed = query.TrimStart();

        // JSON queries start with '{'
        if (trimmed.StartsWith('{'))
        {
            return new MongoJsonQueryParser();
        }

        // Otherwise, use infix parser
        return new InfixQueryParser();
    }
}
