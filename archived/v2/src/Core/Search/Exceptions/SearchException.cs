// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Search.Exceptions;

/// <summary>
/// Exception thrown by SearchService for various error conditions.
/// Includes specific error types for precise error handling (Q19).
/// </summary>
public class SearchException : Exception
{
    /// <summary>
    /// Affected node ID (if applicable).
    /// Null for errors not related to a specific node.
    /// </summary>
    public string? NodeId { get; init; }

    /// <summary>
    /// Type of search error for programmatic handling.
    /// </summary>
    public SearchErrorType ErrorType { get; init; }

    /// <summary>
    /// Initializes a new SearchException.
    /// </summary>
    public SearchException()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new SearchException with a message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public SearchException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new SearchException with message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public SearchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new SearchException with error type.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="errorType">Type of error.</param>
    /// <param name="nodeId">Affected node ID (optional).</param>
    public SearchException(string message, SearchErrorType errorType, string? nodeId = null)
        : base(message)
    {
        this.ErrorType = errorType;
        this.NodeId = nodeId;
    }

    /// <summary>
    /// Initializes a new SearchException with error type and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="errorType">Type of error.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <param name="nodeId">Affected node ID (optional).</param>
    public SearchException(string message, SearchErrorType errorType, Exception innerException, string? nodeId = null)
        : base(message, innerException)
    {
        this.ErrorType = errorType;
        this.NodeId = nodeId;
    }
}

/// <summary>
/// Types of search errors for precise error handling.
/// Allows consumers to handle different error conditions appropriately.
/// </summary>
public enum SearchErrorType
{
    // Node errors (Q19)

    /// <summary>
    /// Node doesn't exist in configuration.
    /// User specified a node that is not configured.
    /// </summary>
    NodeNotFound,

    /// <summary>
    /// User doesn't have access to node.
    /// Node exists but access level prevents operations.
    /// </summary>
    NodeAccessDenied,

    /// <summary>
    /// Node search timed out (Q11).
    /// Node took longer than configured timeout.
    /// </summary>
    NodeTimeout,

    /// <summary>
    /// Node is down or unreachable.
    /// Network error, service unavailable, etc.
    /// </summary>
    NodeUnavailable,

    // Index errors (Requirements #8)

    /// <summary>
    /// Index doesn't exist in node.
    /// User specified an index that is not configured for the node.
    /// </summary>
    IndexNotFound,

    /// <summary>
    /// Index exists but not ready (Q17).
    /// Index may be initializing or building.
    /// </summary>
    IndexUnavailable,

    /// <summary>
    /// Required index is unavailable (Q17).
    /// Index marked as required=true but cannot be used.
    /// </summary>
    IndexRequired,

    // Query errors (Q15, Q16)

    /// <summary>
    /// Malformed query syntax.
    /// Parser could not understand the query.
    /// </summary>
    QuerySyntaxError,

    /// <summary>
    /// Query exceeds complexity limits (Q15).
    /// Too many operators, too deep nesting, etc.
    /// </summary>
    QueryTooComplex,

    /// <summary>
    /// Query parsing timed out.
    /// Prevented potential regex catastrophic backtracking.
    /// </summary>
    QueryTimeout,

    // Validation errors (Requirements #8, Q8)

    /// <summary>
    /// Contradictory configuration.
    /// Example: same node in both --nodes and --exclude-nodes.
    /// </summary>
    InvalidConfiguration,

    /// <summary>
    /// Node prefix references node not in --nodes.
    /// Example: --nodes personal --indexes work:fts-main (work not in nodes list).
    /// </summary>
    InvalidNodePrefix,
}
