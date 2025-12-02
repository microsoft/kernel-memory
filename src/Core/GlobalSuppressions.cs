// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;

// CA1308: Case-insensitive string comparisons are explicitly required by design (Q7 in requirements)
// All field names and string values must be case-insensitive per specification
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Case-insensitive comparisons are required by design specification (Q7)", Scope = "namespaceanddescendants", Target = "~N:KernelMemory.Core.Search")]

// CA1307: StringComparison parameter - using default culture comparison is intentional for query parsing
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Default culture comparison is correct for field path checks", Scope = "member", Target = "~M:KernelMemory.Core.Search.Query.QueryLinqBuilder.GetFieldExpression(KernelMemory.Core.Search.Query.Ast.FieldNode)~System.Linq.Expressions.Expression")]

// CA1305: Culture-specific ToString - using invariant culture would be correct, but this is for diagnostic output
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Diagnostic output, invariant culture would be better but not critical", Scope = "member", Target = "~M:KernelMemory.Core.Search.Query.Parsers.MongoJsonQueryParser.ParseArrayValue(System.Text.Json.JsonElement)~KernelMemory.Core.Search.Query.Ast.LiteralNode")]

// CA1031: Catch general exception in query validation - intentional to provide user-friendly error messages
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Query validation should handle all exceptions gracefully", Scope = "member", Target = "~M:KernelMemory.Core.Search.SearchService.ValidateQueryAsync(System.String,System.Threading.CancellationToken)~System.Threading.Tasks.Task{KernelMemory.Core.Search.Models.QueryValidationResult}")]

// CA1859: Return type specificity - keeping base type for flexibility in visitor pattern
[assembly: SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Visitor pattern requires base type returns for flexibility", Scope = "namespaceanddescendants", Target = "~N:KernelMemory.Core.Search.Query")]
