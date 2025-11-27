// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI;
using KernelMemory.Main.CLI.Commands;
using KernelMemory.Main.CLI.OutputFormatters;
using Moq;
using Xunit;

namespace KernelMemory.Main.Tests.Unit.Commands;

/// <summary>
/// Unit tests for BaseCommand error handling.
/// </summary>
public sealed class BaseCommandTests
{
    [Fact]
    public void HandleError_WithInvalidOperationException_ReturnsUserError()
    {
        // Arrange
        var command = new TestCommand();
        var mockFormatter = new Mock<IOutputFormatter>();
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        var exitCode = command.TestHandleError(exception, mockFormatter.Object);

        // Assert
        Assert.Equal(Constants.ExitCodeUserError, exitCode);
        mockFormatter.Verify(f => f.FormatError("Invalid operation"), Times.Once);
    }

    [Fact]
    public void HandleError_WithArgumentException_ReturnsUserError()
    {
        // Arrange
        var command = new TestCommand();
        var mockFormatter = new Mock<IOutputFormatter>();
        var exception = new ArgumentException("Invalid argument");

        // Act
        var exitCode = command.TestHandleError(exception, mockFormatter.Object);

        // Assert
        Assert.Equal(Constants.ExitCodeUserError, exitCode);
        mockFormatter.Verify(f => f.FormatError("Invalid argument"), Times.Once);
    }

    [Fact]
    public void HandleError_WithGenericException_ReturnsSystemError()
    {
        // Arrange
        var command = new TestCommand();
        var mockFormatter = new Mock<IOutputFormatter>();
        var exception = new Exception("System failure");

        // Act
        var exitCode = command.TestHandleError(exception, mockFormatter.Object);

        // Assert
        Assert.Equal(Constants.ExitCodeSystemError, exitCode);
        mockFormatter.Verify(f => f.FormatError("System failure"), Times.Once);
    }

    [Fact]
    public void HandleError_WithIOException_ReturnsSystemError()
    {
        // Arrange
        var command = new TestCommand();
        var mockFormatter = new Mock<IOutputFormatter>();
        var exception = new System.IO.IOException("File access error");

        // Act
        var exitCode = command.TestHandleError(exception, mockFormatter.Object);

        // Assert
        Assert.Equal(Constants.ExitCodeSystemError, exitCode);
        mockFormatter.Verify(f => f.FormatError("File access error"), Times.Once);
    }

    [Fact]
    public void HandleError_WithNullReferenceException_ReturnsSystemError()
    {
        // Arrange
        var command = new TestCommand();
        var mockFormatter = new Mock<IOutputFormatter>();
        var exception = new NullReferenceException("Null reference");

        // Act
        var exitCode = command.TestHandleError(exception, mockFormatter.Object);

        // Assert
        Assert.Equal(Constants.ExitCodeSystemError, exitCode);
    }

    /// <summary>
    /// Test implementation of BaseCommand to expose protected methods.
    /// </summary>
    private sealed class TestCommand : BaseCommand<GlobalOptions>
    {
        public override Task<int> ExecuteAsync(Spectre.Console.Cli.CommandContext context, GlobalOptions settings)
        {
            throw new NotImplementedException();
        }

        public int TestHandleError(Exception ex, IOutputFormatter formatter)
        {
            return this.HandleError(ex, formatter);
        }
    }
}
