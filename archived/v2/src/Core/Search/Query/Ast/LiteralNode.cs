// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// AST node representing literal values in queries.
/// Supports: strings, numbers, dates, booleans, arrays.
/// </summary>
public sealed class LiteralNode : QueryNode
{
    /// <summary>
    /// The literal value.
    /// Can be: string, int, float, DateTimeOffset, bool, or array of these types.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// The type of the literal value for type-safe operations.
    /// </summary>
    public Type ValueType => this.Value.GetType();

    /// <summary>
    /// True if the value is a string.
    /// </summary>
    public bool IsString => this.Value is string;

    /// <summary>
    /// True if the value is a number (int, long, float, double, decimal).
    /// </summary>
    public bool IsNumber => this.Value is int or long or float or double or decimal;

    /// <summary>
    /// True if the value is a date/time.
    /// </summary>
    public bool IsDateTime => this.Value is DateTimeOffset or DateTime;

    /// <summary>
    /// True if the value is a boolean.
    /// </summary>
    public bool IsBoolean => this.Value is bool;

    /// <summary>
    /// True if the value is an array.
    /// </summary>
    public bool IsArray => this.Value is Array or System.Collections.IList;

    /// <summary>
    /// Get the value as a string.
    /// Throws if not a string.
    /// </summary>
    public string AsString()
    {
        return (string)this.Value;
    }

    /// <summary>
    /// Get the value as a DateTimeOffset.
    /// Converts DateTime to DateTimeOffset if needed.
    /// Throws if not a date/time.
    /// </summary>
    public DateTimeOffset AsDateTime()
    {
        return this.Value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            _ => throw new InvalidOperationException($"Value is not a DateTime: {this.ValueType.Name}")
        };
    }

    /// <summary>
    /// Get the value as a number (double).
    /// Throws if not a number.
    /// </summary>
    public double AsNumber()
    {
        return this.Value switch
        {
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal m => (double)m,
            _ => throw new InvalidOperationException($"Value is not a number: {this.ValueType.Name}")
        };
    }

    /// <summary>
    /// Get the value as an array of strings.
    /// Throws if not an array.
    /// </summary>
    public string[] AsStringArray()
    {
        if (this.Value is string[] stringArray)
        {
            return stringArray;
        }

        if (this.Value is System.Collections.IList list)
        {
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = list[i]?.ToString() ?? string.Empty;
            }

            return result;
        }

        throw new InvalidOperationException($"Value is not an array: {this.ValueType.Name}");
    }

    /// <summary>
    /// Accept a visitor for AST traversal.
    /// </summary>
    public override T Accept<T>(IQueryNodeVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }
}
