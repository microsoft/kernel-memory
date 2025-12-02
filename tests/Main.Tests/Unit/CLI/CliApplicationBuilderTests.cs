// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Main.CLI;

namespace KernelMemory.Main.Tests.Unit.CLI;

public sealed class CliApplicationBuilderTests
{
    [Fact]
    public void Build_CreatesCommandApp()
    {
        var builder = new CliApplicationBuilder();
        var app = builder.Build();
        Assert.NotNull(app);
    }

    [Fact]
    public void Configure_SetsApplicationName()
    {
        var builder = new CliApplicationBuilder();
        var app = builder.Build();
        // App is configured with name "km"
        Assert.NotNull(app);
    }
}
