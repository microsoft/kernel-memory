// Copyright (c) Microsoft. All rights reserved.
using System.Linq.Expressions;
using KernelMemory.Core.Search.Query.Ast;

namespace KernelMemory.Core.Search.Query;

/// <summary>
/// Transforms query AST into LINQ expressions for EF Core.
/// Handles NoSQL semantics: missing fields, case-insensitive comparisons, metadata dot notation.
/// </summary>
public sealed class QueryLinqBuilder : IQueryNodeVisitor<Expression>
{
    private readonly ParameterExpression _parameter;
    private readonly Type _recordType;

    /// <summary>
    /// Initialize a new QueryLinqBuilder.
    /// </summary>
    /// <param name="recordType">The record type to build expressions for (ContentRecord).</param>
    public QueryLinqBuilder(Type recordType)
    {
        this._recordType = recordType;
        this._parameter = Expression.Parameter(recordType, "x");
    }

    /// <summary>
    /// Build a LINQ expression from a query AST.
    /// </summary>
    /// <param name="queryNode">The root query node.</param>
    /// <returns>A LINQ expression tree: Expression&lt;Func&lt;ContentRecord, bool&gt;&gt;</returns>
    public Expression<Func<T, bool>> Build<T>(QueryNode queryNode) where T : class
    {
        if (this._recordType != typeof(T))
        {
            throw new ArgumentException($"Type mismatch: builder is for {this._recordType.Name}, requested {typeof(T).Name}");
        }

        var body = queryNode.Accept(this);
        return (Expression<Func<T, bool>>)Expression.Lambda(body, this._parameter);
    }

    /// <summary>
    /// Visit a logical node (AND, OR, NOT, NOR).
    /// </summary>
    public Expression Visit(LogicalNode node)
    {
        if (node.Children.Length == 0)
        {
            throw new ArgumentException("Logical node must have at least one child");
        }

        return node.Operator switch
        {
            LogicalOperator.And => this.BuildAnd(node.Children),
            LogicalOperator.Or => this.BuildOr(node.Children),
            LogicalOperator.Not => this.BuildNot(node.Children[0]),
            LogicalOperator.Nor => this.BuildNor(node.Children),
            _ => throw new ArgumentException($"Unknown logical operator: {node.Operator}")
        };
    }

    /// <summary>
    /// Visit a comparison node (==, !=, >=, etc.).
    /// </summary>
    public Expression Visit(ComparisonNode node)
    {
        var field = node.Field;
        var op = node.Operator;
        var value = node.Value;

        // Get the field expression (property access)
        Expression fieldExpr = this.GetFieldExpression(field);

        // Special handling for Exists operator
        if (op == ComparisonOperator.Exists)
        {
            return this.BuildExistsCheck(field, value?.Value is true);
        }

        if (value == null)
        {
            throw new ArgumentException("Comparison value cannot be null (except for Exists operator)");
        }

        // Handle metadata fields specially
        if (field.IsMetadataField)
        {
            return this.BuildMetadataComparison(field, op, value);
        }

        // Handle In operator
        if (op == ComparisonOperator.In || op == ComparisonOperator.NotIn)
        {
            return this.BuildInComparison(fieldExpr, op, value);
        }

        // Handle Contains operator (regex/FTS)
        if (op == ComparisonOperator.Contains)
        {
            return this.BuildContainsComparison(fieldExpr, value);
        }

        // Standard comparison operators
        return this.BuildStandardComparison(fieldExpr, op, value);
    }

    /// <summary>
    /// Visit a text search node (FTS search across all fields).
    /// </summary>
    public Expression Visit(TextSearchNode node)
    {
        // If specific field, search that field only
        if (node.Field != null)
        {
            var fieldExpr = this.GetFieldExpression(node.Field);
            return this.BuildContainsComparison(fieldExpr, new LiteralNode { Value = node.SearchText });
        }

        // Default field behavior: search across all FTS-indexed fields (title, description, content)
        var titleProp = Expression.Property(this._parameter, "Title");
        var descProp = Expression.Property(this._parameter, "Description");
        var contentProp = Expression.Property(this._parameter, "Content");

        var searchValue = node.SearchText.ToLowerInvariant();
        var searchExpr = Expression.Constant(searchValue);

        // Title contains (with null check)
        var titleNotNull = Expression.NotEqual(titleProp, Expression.Constant(null, typeof(string)));
        var titleLower = Expression.Call(titleProp, typeof(string).GetMethod("ToLowerInvariant")!);
        var titleContains = Expression.Call(titleLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, searchExpr);
        var titleMatch = Expression.AndAlso(titleNotNull, titleContains);

        // Description contains (with null check)
        var descNotNull = Expression.NotEqual(descProp, Expression.Constant(null, typeof(string)));
        var descLower = Expression.Call(descProp, typeof(string).GetMethod("ToLowerInvariant")!);
        var descContains = Expression.Call(descLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, searchExpr);
        var descMatch = Expression.AndAlso(descNotNull, descContains);

        // Content contains (always required, but check anyway)
        var contentNotNull = Expression.NotEqual(contentProp, Expression.Constant(null, typeof(string)));
        var contentLower = Expression.Call(contentProp, typeof(string).GetMethod("ToLowerInvariant")!);
        var contentContains = Expression.Call(contentLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, searchExpr);
        var contentMatch = Expression.AndAlso(contentNotNull, contentContains);

        // OR them together: title matches OR description matches OR content matches
        return Expression.OrElse(Expression.OrElse(titleMatch, descMatch), contentMatch);
    }

    /// <summary>
    /// Visit a field node (not used directly, but required by interface).
    /// </summary>
    public Expression Visit(FieldNode node)
    {
        return this.GetFieldExpression(node);
    }

    /// <summary>
    /// Visit a literal node (not used directly, but required by interface).
    /// </summary>
    public Expression Visit(LiteralNode node)
    {
        return Expression.Constant(node.Value);
    }

    // Helper methods for building expressions

    private Expression BuildAnd(QueryNode[] children)
    {
        var exprs = children.Select(c => c.Accept(this)).ToArray();
        return exprs.Aggregate((left, right) => Expression.AndAlso(left, right));
    }

    private Expression BuildOr(QueryNode[] children)
    {
        var exprs = children.Select(c => c.Accept(this)).ToArray();
        return exprs.Aggregate((left, right) => Expression.OrElse(left, right));
    }

    private Expression BuildNot(QueryNode child)
    {
        return Expression.Not(child.Accept(this));
    }

    private Expression BuildNor(QueryNode[] children)
    {
        // NOR = NOT (child1 OR child2 OR ...)
        return Expression.Not(this.BuildOr(children));
    }

    private Expression GetFieldExpression(FieldNode field)
    {
        // Simple field: direct property access
        if (!field.FieldPath.Contains('.'))
        {
            return Expression.Property(this._parameter, this.GetPropertyName(field.FieldPath));
        }

        // Dot notation: handle metadata access
        if (field.IsMetadataField)
        {
            // For metadata, we'll handle it specially in BuildMetadataComparison
            return Expression.Property(this._parameter, "Metadata");
        }

        // Nested field (not metadata): not supported
        throw new NotSupportedException($"Nested field access not supported: {field.FieldPath}");
    }

    private string GetPropertyName(string fieldPath)
    {
        // Normalize field names to property names (case-insensitive matching)
        return fieldPath.ToLowerInvariant() switch
        {
            "id" => "Id",
            "title" => "Title",
            "description" => "Description",
            "content" => "Content",
            "tags" => "Tags",
            "mimetype" => "MimeType",
            "createdat" => "CreatedAt",
            "metadata" => "Metadata",
            _ => throw new ArgumentException($"Unknown field: {fieldPath}")
        };
    }

    private Expression BuildMetadataComparison(FieldNode field, ComparisonOperator op, LiteralNode value)
    {
        var metadataKey = field.MetadataKey ?? throw new InvalidOperationException("Metadata key cannot be null");

        // Get Metadata dictionary property
        var metadataProp = Expression.Property(this._parameter, "Metadata");

        // Check if key exists: Metadata.ContainsKey(key)
        var containsKeyMethod = typeof(Dictionary<string, string>).GetMethod("ContainsKey")!;
        var keyExpr = Expression.Constant(metadataKey);
        var containsKey = Expression.Call(metadataProp, containsKeyMethod, keyExpr);

        // Get value: Metadata[key]
        var indexer = typeof(Dictionary<string, string>).GetProperty("Item")!;
        var getValue = Expression.Property(metadataProp, indexer, keyExpr);

        // Case-insensitive comparison
        var valueStr = value.Value.ToString() ?? string.Empty;
        var valueExpr = Expression.Constant(valueStr.ToLowerInvariant());
        var toLowerMethod = typeof(string).GetMethod("ToLowerInvariant")!;
        var valueLower = Expression.Call(getValue, toLowerMethod);

        // Build comparison
        Expression comparison = op switch
        {
            ComparisonOperator.Equal => Expression.Equal(valueLower, valueExpr),
            ComparisonOperator.NotEqual => Expression.NotEqual(valueLower, valueExpr),
            ComparisonOperator.Contains => Expression.Call(
                valueLower,
                typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                valueExpr),
            _ => throw new NotSupportedException($"Operator {op} not supported for metadata fields")
        };

        // NoSQL semantics:
        // Positive match (==, Contains): return records that HAVE the key AND match
        // Negative match (!=): return records that DON'T have the key OR have different value
        if (op == ComparisonOperator.NotEqual)
        {
            // NOT has key OR (has key AND value differs)
            var notHasKey = Expression.Not(containsKey);
            var hasKeyAndDiffers = Expression.AndAlso(containsKey, comparison);
            return Expression.OrElse(notHasKey, hasKeyAndDiffers);
        }
        else
        {
            // Has key AND comparison succeeds
            return Expression.AndAlso(containsKey, comparison);
        }
    }

    private Expression BuildExistsCheck(FieldNode field, bool shouldExist)
    {
        if (!field.IsMetadataField)
        {
            // For regular fields, check if not null
            var fieldExpr = this.GetFieldExpression(field);
            var notNull = Expression.NotEqual(fieldExpr, Expression.Constant(null));
            return shouldExist ? notNull : Expression.Not(notNull);
        }

        // For metadata, check dictionary key
        var metadataKey = field.MetadataKey ?? throw new InvalidOperationException("Metadata key cannot be null");
        var metadataProp = Expression.Property(this._parameter, "Metadata");
        var containsKeyMethod = typeof(Dictionary<string, string>).GetMethod("ContainsKey")!;
        var keyExpr = Expression.Constant(metadataKey);
        var containsKey = Expression.Call(metadataProp, containsKeyMethod, keyExpr);

        return shouldExist ? containsKey : Expression.Not(containsKey);
    }

    private Expression BuildInComparison(Expression fieldExpr, ComparisonOperator op, LiteralNode value)
    {
        var array = value.AsStringArray();

        // For tags field (string array), check if any tag is in the search array
        if (fieldExpr.Type == typeof(string[]))
        {
            // tags.Any(t => searchArray.Contains(t))
            var searchArray = Expression.Constant(array.Select(s => s.ToLowerInvariant()).ToArray());
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var tagParam = Expression.Parameter(typeof(string), "t");
            var tagLower = Expression.Call(tagParam, typeof(string).GetMethod("ToLowerInvariant")!);
            var inArray = Expression.Call(containsMethod, searchArray, tagLower);
            var predicate = Expression.Lambda<Func<string, bool>>(inArray, tagParam);
            var anyCall = Expression.Call(anyMethod, fieldExpr, predicate);

            return op == ComparisonOperator.In ? anyCall : Expression.Not(anyCall);
        }

        // For regular string fields, check if value is in array
        var lowerMethod = typeof(string).GetMethod("ToLowerInvariant")!;
        var fieldLower = Expression.Call(fieldExpr, lowerMethod);
        var arrayExpr = Expression.Constant(array.Select(s => s.ToLowerInvariant()).ToArray());
        var containsMethodSingle = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));
        var contains = Expression.Call(containsMethodSingle, arrayExpr, fieldLower);

        return op == ComparisonOperator.In ? contains : Expression.Not(contains);
    }

    private Expression BuildContainsComparison(Expression fieldExpr, LiteralNode value)
    {
        var searchStr = value.AsString().ToLowerInvariant();
        var searchExpr = Expression.Constant(searchStr);

        // Null check for optional fields
        var notNull = Expression.NotEqual(fieldExpr, Expression.Constant(null, fieldExpr.Type));

        // String.ToLowerInvariant().Contains(searchStr)
        var toLowerMethod = typeof(string).GetMethod("ToLowerInvariant")!;
        var lower = Expression.Call(fieldExpr, toLowerMethod);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var contains = Expression.Call(lower, containsMethod, searchExpr);

        return Expression.AndAlso(notNull, contains);
    }

    private Expression BuildStandardComparison(Expression fieldExpr, ComparisonOperator op, LiteralNode value)
    {
        // Convert value to appropriate type
        Expression valueExpr;

        if (fieldExpr.Type == typeof(DateTimeOffset) || fieldExpr.Type == typeof(DateTimeOffset?))
        {
            valueExpr = Expression.Constant(value.AsDateTime());
        }
        else if (fieldExpr.Type == typeof(string))
        {
            // Case-insensitive string comparison
            var searchStr = value.AsString().ToLowerInvariant();
            valueExpr = Expression.Constant(searchStr);
            var toLowerMethod = typeof(string).GetMethod("ToLowerInvariant")!;
            fieldExpr = Expression.Call(fieldExpr, toLowerMethod);
        }
        else
        {
            valueExpr = Expression.Constant(value.Value, fieldExpr.Type);
        }

        return op switch
        {
            ComparisonOperator.Equal => Expression.Equal(fieldExpr, valueExpr),
            ComparisonOperator.NotEqual => Expression.NotEqual(fieldExpr, valueExpr),
            ComparisonOperator.GreaterThan => Expression.GreaterThan(fieldExpr, valueExpr),
            ComparisonOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(fieldExpr, valueExpr),
            ComparisonOperator.LessThan => Expression.LessThan(fieldExpr, valueExpr),
            ComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(fieldExpr, valueExpr),
            _ => throw new NotSupportedException($"Operator {op} not supported for standard comparison")
        };
    }
}
