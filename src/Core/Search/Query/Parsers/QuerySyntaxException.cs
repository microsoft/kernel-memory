// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Query.Parsers;

/// <summary>
/// Exception thrown when query parsing fails due to syntax errors.
/// </summary>
public class QuerySyntaxException : Exception
{
    /// <summary>
    /// Character position where the error occurred (0-based).
    /// Null if position is unknown.
    /// </summary>
    public int? Position { get; init; }

    /// <summary>
    /// Expected token or syntax element.
    /// </summary>
    public string? ExpectedToken { get; init; }

    /// <summary>
    /// Actual token found at error position.
    /// </summary>
    public string? ActualToken { get; init; }

    /// <summary>
    /// Initialize a new QuerySyntaxException.
    /// </summary>
    public QuerySyntaxException()
        : base()
    {
    }

    /// <summary>
    /// Initialize a new QuerySyntaxException with a message.
    /// </summary>
    public QuerySyntaxException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initialize a new QuerySyntaxException with position.
    /// </summary>
    public QuerySyntaxException(string message, int position) : base(message)
    {
        this.Position = position;
    }

    /// <summary>
    /// Initialize a new QuerySyntaxException with position and expected/actual tokens.
    /// </summary>
    public QuerySyntaxException(
        string message,
        int position,
        string? expectedToken,
        string? actualToken) : base(message)
    {
        this.Position = position;
        this.ExpectedToken = expectedToken;
        this.ActualToken = actualToken;
    }

    /// <summary>
    /// Initialize a new QuerySyntaxException with inner exception.
    /// </summary>
    public QuerySyntaxException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
