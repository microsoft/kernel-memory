// Copyright (c) Microsoft. All rights reserved.
using KernelMemory.Main.CLI;
using KernelMemory.Main.CLI.Commands;
using KernelMemory.Main.CLI.OutputFormatters;
using Moq;
using Spectre.Console.Cli;

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
        var exception = new System.IO.IOException("System failure");

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

    /// <summary>
    /// Test implementation of BaseCommand to expose protected methods.
    /// </summary>
    private sealed class TestCommand : BaseCommand<GlobalOptions>
    {
        public TestCommand() : base(CreateTestConfig())
        {
        }

        public override Task<int> ExecuteAsync(CommandContext context, GlobalOptions settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public int TestHandleError(Exception ex, IOutputFormatter formatter)
        {
            return this.HandleError(ex, formatter);
        }

        private static KernelMemory.Core.Config.AppConfig CreateTestConfig()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid()}");
            return new KernelMemory.Core.Config.AppConfig
            {
                Nodes = new Dictionary<string, KernelMemory.Core.Config.NodeConfig>
                {
                    ["test"] = KernelMemory.Core.Config.NodeConfig.CreateDefaultPersonalNode(tempDir)
                }
            };
        }
    }
}
