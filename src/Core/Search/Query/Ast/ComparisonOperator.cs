// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// Comparison operators supported in queries.
/// Maps to both infix syntax and MongoDB JSON operators.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>Equality: field:value or field==value or $eq</summary>
    Equal,

    /// <summary>Inequality: field!=value or $ne</summary>
    NotEqual,

    /// <summary>Greater than: field>value or $gt</summary>
    GreaterThan,

    /// <summary>Greater than or equal: field>=value or $gte</summary>
    GreaterThanOrEqual,

    /// <summary>Less than: field&lt;value or $lt</summary>
    LessThan,

    /// <summary>Less than or equal: field&lt;=value or $lte</summary>
    LessThanOrEqual,

    /// <summary>Contains/Regex: field:~"pattern" or $regex</summary>
    Contains,

    /// <summary>Array contains any: field:[value1,value2] or $in</summary>
    In,

    /// <summary>Not in array: $nin</summary>
    NotIn,

    /// <summary>Field exists: $exists</summary>
    Exists
}
