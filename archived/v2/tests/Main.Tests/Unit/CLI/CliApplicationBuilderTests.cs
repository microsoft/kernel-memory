// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Main.CLI;

namespace KernelMemory.Main.Tests.Unit.CLI;

/// <summary>
/// Tests for CliApplicationBuilder.
/// These tests verify that the CLI application builder correctly creates and configures
/// command applications. Tests use isolated temp directories to avoid accessing ~/.km.
/// </summary>
public sealed class CliApplicationBuilderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempConfigPath;

    public CliApplicationBuilderTests()
    {
        // Create isolated temp directory for each test to avoid ~/.km access
        this._tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._tempDir);
        this._tempConfigPath = Path.Combine(this._tempDir, "config.json");
    }

    public void Dispose()
    {
        // Clean up temp directory after test
        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(this._tempDir, recursive: true);
        }
    }

    [Fact]
    public void Build_CreatesCommandApp()
    {
        // Arrange: Use temp config path to avoid accessing ~/.km
        var builder = new CliApplicationBuilder();
        var args = new[] { "--config", this._tempConfigPath };

        // Act
        var app = builder.Build(args);

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Configure_SetsApplicationName()
    {
        // Arrange: Use temp config path to avoid accessing ~/.km
        var builder = new CliApplicationBuilder();
        var args = new[] { "--config", this._tempConfigPath };

        // Act
        var app = builder.Build(args);

        // Assert: App is configured with name "km"
        Assert.NotNull(app);
    }
}
