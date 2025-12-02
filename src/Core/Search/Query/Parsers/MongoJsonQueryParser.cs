// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Text.Json;
using KernelMemory.Core.Search.Query.Ast;

namespace KernelMemory.Core.Search.Query.Parsers;

/// <summary>
/// Parser for MongoDB JSON query format.
/// Supports subset of MongoDB query operators: $and, $or, $not, $nor, $eq, $ne, $gt, $gte, $lt, $lte, $in, $nin, $regex, $text, $exists.
/// Examples: {"content": {"$regex": "kubernetes"}}, {"$and": [{"tags": "AI"}, {"createdAt": {"$gte": "2024-01-01"}}]}
/// </summary>
public sealed class MongoJsonQueryParser : IQueryParser
{
    /// <summary>
    /// Parse a MongoDB JSON query string into an AST.
    /// </summary>
    public QueryNode Parse(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new QuerySyntaxException("Query cannot be empty");
        }

        try
        {
            using var doc = JsonDocument.Parse(query);
            return this.ParseElement(doc.RootElement);
        }
        catch (JsonException ex)
        {
            throw new QuerySyntaxException("Invalid JSON format", ex);
        }
        catch (QuerySyntaxException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new QuerySyntaxException("Failed to parse MongoDB query", ex);
        }
    }

    /// <summary>
    /// Validate query syntax without full parsing.
    /// </summary>
    public bool Validate(string query)
    {
        try
        {
            this.Parse(query);
            return true;
        }
        catch (QuerySyntaxException)
        {
            return false;
        }
    }

    /// <summary>
    /// Parse a JSON element into a query node.
    /// </summary>
    private QueryNode ParseElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new QuerySyntaxException("Query must be a JSON object");
        }

        var conditions = new List<QueryNode>();

        foreach (var property in element.EnumerateObject())
        {
            var name = property.Name;
            var value = property.Value;

            // Special $text operator (full-text search) - check before other $ operators
            if (name == "$text")
            {
                conditions.Add(this.ParseTextSearch(value));
            }
            // Logical operators
            else if (name.StartsWith('$'))
            {
                conditions.Add(this.ParseLogicalOperator(name, value));
            }
            // Field comparison
            else
            {
                conditions.Add(this.ParseFieldComparison(name, value));
            }
        }

        // If multiple conditions at root level, they are implicitly AND'ed
        if (conditions.Count == 0)
        {
            throw new QuerySyntaxException("Query cannot be empty");
        }

        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        return new LogicalNode
        {
            Operator = LogicalOperator.And,
            Children = [.. conditions]
        };
    }

    /// <summary>
    /// Parse a logical operator ($and, $or, $not, $nor).
    /// </summary>
    private QueryNode ParseLogicalOperator(string operatorName, JsonElement value)
    {
        return operatorName switch
        {
            "$and" => this.ParseAndOr(LogicalOperator.And, value),
            "$or" => this.ParseAndOr(LogicalOperator.Or, value),
            "$nor" => this.ParseAndOr(LogicalOperator.Nor, value),
            "$not" => this.ParseNot(value),
            _ => throw new QuerySyntaxException($"Unknown logical operator: {operatorName}")
        };
    }

    /// <summary>
    /// Parse $and, $or, or $nor (array of conditions).
    /// </summary>
    private QueryNode ParseAndOr(LogicalOperator op, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new QuerySyntaxException($"${op} requires an array of conditions");
        }

        var children = new List<QueryNode>();
        foreach (var element in value.EnumerateArray())
        {
            children.Add(this.ParseElement(element));
        }

        if (children.Count == 0)
        {
            throw new QuerySyntaxException($"${op} requires at least one condition");
        }

        return new LogicalNode
        {
            Operator = op,
            Children = [.. children]
        };
    }

    /// <summary>
    /// Parse $not (single condition).
    /// </summary>
    private QueryNode ParseNot(JsonElement value)
    {
        return new LogicalNode
        {
            Operator = LogicalOperator.Not,
            Children = [this.ParseElement(value)]
        };
    }

    /// <summary>
    /// Parse $text operator (full-text search).
    /// </summary>
    private QueryNode ParseTextSearch(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new QuerySyntaxException("$text requires an object");
        }

        string? searchText = null;

        foreach (var prop in value.EnumerateObject())
        {
            if (prop.Name == "$search")
            {
                searchText = prop.Value.GetString();
            }
        }

        if (string.IsNullOrEmpty(searchText))
        {
            throw new QuerySyntaxException("$text requires a $search property");
        }

        return new TextSearchNode
        {
            SearchText = searchText,
            Field = null
        };
    }

    /// <summary>
    /// Parse a field comparison (field: value or field: {$op: value}).
    /// </summary>
    private QueryNode ParseFieldComparison(string fieldPath, JsonElement value)
    {
        var field = new FieldNode { FieldPath = fieldPath.ToLowerInvariant() };

        // Simple equality: {"field": "value"}
        if (value.ValueKind != JsonValueKind.Object)
        {
            return new ComparisonNode
            {
                Field = field,
                Operator = ComparisonOperator.Equal,
                Value = this.ParseLiteralValue(value)
            };
        }

        // Operator object: {"field": {"$op": value}}
        var conditions = new List<QueryNode>();

        foreach (var prop in value.EnumerateObject())
        {
            var opName = prop.Name;
            var opValue = prop.Value;

            if (!opName.StartsWith('$'))
            {
                throw new QuerySyntaxException($"Expected operator (starting with $), got: {opName}");
            }

            var compOp = opName switch
            {
                "$eq" => ComparisonOperator.Equal,
                "$ne" => ComparisonOperator.NotEqual,
                "$gt" => ComparisonOperator.GreaterThan,
                "$gte" => ComparisonOperator.GreaterThanOrEqual,
                "$lt" => ComparisonOperator.LessThan,
                "$lte" => ComparisonOperator.LessThanOrEqual,
                "$in" => ComparisonOperator.In,
                "$nin" => ComparisonOperator.NotIn,
                "$regex" => ComparisonOperator.Contains,
                "$exists" => ComparisonOperator.Exists,
                _ => throw new QuerySyntaxException($"Unknown comparison operator: {opName}")
            };

            // $exists is special - value is boolean
            if (compOp == ComparisonOperator.Exists)
            {
                var exists = opValue.GetBoolean();
                var existsNode = new ComparisonNode
                {
                    Field = field,
                    Operator = ComparisonOperator.Exists,
                    Value = new LiteralNode { Value = exists }
                };

                // If exists: false, wrap in NOT
                if (!exists)
                {
                    conditions.Add(new LogicalNode
                    {
                        Operator = LogicalOperator.Not,
                        Children = [existsNode]
                    });
                }
                else
                {
                    conditions.Add(existsNode);
                }
            }
            else
            {
                conditions.Add(new ComparisonNode
                {
                    Field = field,
                    Operator = compOp,
                    Value = this.ParseLiteralValue(opValue)
                });
            }
        }

        // Multiple operators on same field are implicitly AND'ed
        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        return new LogicalNode
        {
            Operator = LogicalOperator.And,
            Children = [.. conditions]
        };
    }

    /// <summary>
    /// Parse a literal value from JSON.
    /// </summary>
    private LiteralNode ParseLiteralValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new LiteralNode { Value = element.GetString() ?? string.Empty },
            JsonValueKind.Number => new LiteralNode { Value = element.GetDouble() },
            JsonValueKind.True => new LiteralNode { Value = true },
            JsonValueKind.False => new LiteralNode { Value = false },
            JsonValueKind.Array => this.ParseArrayValue(element),
            _ => throw new QuerySyntaxException($"Unsupported value type: {element.ValueKind}")
        };
    }

    /// <summary>
    /// Parse an array value from JSON.
    /// </summary>
    private LiteralNode ParseArrayValue(JsonElement element)
    {
        var items = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                items.Add(item.GetString() ?? string.Empty);
            }
            else if (item.ValueKind == JsonValueKind.Number)
            {
                items.Add(item.GetDouble().ToString(CultureInfo.CurrentCulture));
            }
            else
            {
                items.Add(item.ToString());
            }
        }

        return new LiteralNode { Value = items.ToArray() };
    }
}
