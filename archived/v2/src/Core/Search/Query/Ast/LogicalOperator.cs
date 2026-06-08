// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Ast;

/// <summary>
/// Logical operators for combining query conditions.
/// Maps to both infix syntax (AND, OR, NOT) and MongoDB operators ($and, $or, $not, $nor).
/// </summary>
public enum LogicalOperator
{
    /// <summary>Logical AND: all conditions must be true</summary>
    And,

    /// <summary>Logical OR: at least one condition must be true</summary>
    Or,

    /// <summary>Logical NOT: negates the condition</summary>
    Not,

    /// <summary>Logical NOR: none of the conditions are true (MongoDB only)</summary>
    Nor
}
