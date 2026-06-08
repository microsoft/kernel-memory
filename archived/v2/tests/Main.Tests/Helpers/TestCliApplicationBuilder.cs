// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Core.Config;
using KernelMemory.Main.CLI;
using KernelMemory.Main.CLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace KernelMemory.Main.Tests.Helpers;

/// <summary>
/// Test builder for CLI application that allows injecting custom AppConfig without file I/O.
/// Ensures tests never touch user's personal ~/.km directory.
/// </summary>
public sealed class TestCliApplicationBuilder
{
    private AppConfig? _testConfig;

    /// <summary>
    /// Injects a custom AppConfig for testing (no file I/O required).
    /// </summary>
    /// <param name="config">Test configuration to inject.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public TestCliApplicationBuilder WithConfig(AppConfig config)
    {
        this._testConfig = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <summary>
    /// Builds a CommandApp with injected test config (no file I/O).
    /// </summary>
    /// <returns>A configured CommandApp ready for testing.</returns>
    public CommandApp Build()
    {
        // Use test config or create a minimal default
        var config = this._testConfig ?? this.CreateMinimalTestConfig();

        // Create DI container and register config
        var services = new ServiceCollection();
        services.AddSingleton(config);

        // Create type registrar for Spectre.Console.Cli DI integration
        var registrar = new TypeRegistrar(services);

        // Build CommandApp with DI
        var app = new CommandApp(registrar);

        // Reuse production command configuration
        var builder = new CliApplicationBuilder();
        builder.Configure(app);

        return app;
    }

    /// <summary>
    /// Creates a minimal test config pointing to temp directory (never ~/.km).
    /// </summary>
    /// <returns>A minimal test configuration.</returns>
    private AppConfig CreateMinimalTestConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"km-test-{Guid.NewGuid()}");
        return new AppConfig
        {
            Nodes = new Dictionary<string, NodeConfig>
            {
                ["test"] = NodeConfig.CreateDefaultPersonalNode(
                    Path.Combine(tempDir, "nodes", "test"))
            }
        };
    }
}
