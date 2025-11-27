// Copyright (c) Microsoft. All rights reserved.
namespace KernelMemory.Core.Storage.Exceptions;

/// <summary>
/// Base exception for content storage errors.
/// </summary>
public class ContentStorageException : Exception
{
    public ContentStorageException()
    {
    }

    public ContentStorageException(string message) : base(message)
    {
    }

    public ContentStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a content record is not found.
/// </summary>
public class ContentNotFoundException : ContentStorageException
{
    public string ContentId { get; }

    public ContentNotFoundException(string contentId)
        : base($"Content with ID '{contentId}' was not found.")
    {
        this.ContentId = contentId;
    }

    public ContentNotFoundException(string contentId, string message)
        : base(message)
    {
        this.ContentId = contentId;
    }

    public ContentNotFoundException() : base()
    {
        this.ContentId = string.Empty;
    }

    public ContentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
        this.ContentId = string.Empty;
    }
}

/// <summary>
/// Exception thrown when an operation fails during processing.
/// </summary>
public class OperationFailedException : ContentStorageException
{
    public string OperationId { get; }

    public OperationFailedException(string operationId, string message)
        : base(message)
    {
        this.OperationId = operationId;
    }

    public OperationFailedException(string operationId, string message, Exception innerException)
        : base(message, innerException)
    {
        this.OperationId = operationId;
    }

    public OperationFailedException() : base()
    {
        this.OperationId = string.Empty;
    }

    public OperationFailedException(string message) : base(message)
    {
        this.OperationId = string.Empty;
    }

    public OperationFailedException(string message, Exception innerException) : base(message, innerException)
    {
        this.OperationId = string.Empty;
    }
}
