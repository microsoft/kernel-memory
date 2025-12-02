// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Storage.Exceptions;

namespace KernelMemory.Core.Tests.Storage;

/// <summary>
/// Tests for storage exception classes.
/// </summary>
public sealed class StorageExceptionsTests
{
    [Fact]
    public void ContentStorageException_WithMessage_CreatesException()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new ContentStorageException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ContentStorageException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        const string message = "Test error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ContentStorageException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ContentNotFoundException_WithContentId_CreatesException()
    {
        // Arrange
        const string contentId = "test-id-123";

        // Act
        var exception = new ContentNotFoundException(contentId);

        // Assert
        Assert.Contains(contentId, exception.Message);
        Assert.Equal(contentId, exception.ContentId);
        Assert.IsAssignableFrom<ContentStorageException>(exception);
    }

    [Fact]
    public void ContentNotFoundException_WithContentIdAndCustomMessage_CreatesException()
    {
        // Arrange
        const string contentId = "test-id-456";
        const string customMessage = "Custom error message";

        // Act
        var exception = new ContentNotFoundException(contentId, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(contentId, exception.ContentId);
    }

    [Fact]
    public void ContentNotFoundException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        const string message = "Content not found";
        var innerException = new FileNotFoundException("File not found");

        // Act
        var exception = new ContentNotFoundException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void OperationFailedException_WithOperationIdAndMessage_CreatesException()
    {
        // Arrange
        const string operationId = "op-123";
        const string message = "Operation failed";

        // Act
        var exception = new OperationFailedException(operationId, message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(operationId, exception.OperationId);
        Assert.IsAssignableFrom<ContentStorageException>(exception);
    }

    [Fact]
    public void OperationFailedException_WithOperationIdMessageAndInnerException_CreatesException()
    {
        // Arrange
        const string operationId = "op-456";
        const string message = "Operation failed";
        var innerException = new TimeoutException("Timeout");

        // Act
        var exception = new OperationFailedException(operationId, message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(operationId, exception.OperationId);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void OperationFailedException_WithMessage_CreatesExceptionWithEmptyOperationId()
    {
        // Arrange
        const string message = "Operation failed";

        // Act
        var exception = new OperationFailedException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(string.Empty, exception.OperationId);
    }
}
