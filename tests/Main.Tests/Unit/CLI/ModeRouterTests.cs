// Copyright (c) Microsoft. All rights reserved.

using KernelMemory.Main.CLI;

namespace KernelMemory.Main.Tests.Unit.CLI;

public sealed class ModeRouterTests
{
    [Fact]
    public void DetectMode_NoArgs_ReturnsCli()
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(Array.Empty<string>());
        Assert.Equal("cli", mode);
    }

    [Theory]
    [InlineData("mcp", "mcp")]
    [InlineData("mcpserver", "mcp")]
    [InlineData("MCP", "mcp")]
    [InlineData("MCPSERVER", "mcp")]
    public void DetectMode_McpArguments_ReturnsMcp(string arg, string expected)
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(new[] { arg });
        Assert.Equal(expected, mode);
    }

    [Theory]
    [InlineData("web", "web")]
    [InlineData("webservice", "web")]
    [InlineData("WEB", "web")]
    [InlineData("WEBSERVICE", "web")]
    public void DetectMode_WebArguments_ReturnsWeb(string arg, string expected)
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(new[] { arg });
        Assert.Equal(expected, mode);
    }

    [Theory]
    [InlineData("rpc")]
    [InlineData("RPC")]
    public void DetectMode_RpcArguments_ReturnsRpc(string arg)
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(new[] { arg });
        Assert.Equal("rpc", mode);
    }

    [Theory]
    [InlineData("put", "cli")]
    [InlineData("get", "cli")]
    [InlineData("list", "cli")]
    [InlineData("unknown", "cli")]
    [InlineData("--help", "cli")]
    public void DetectMode_OtherArguments_ReturnsCli(string arg, string expected)
    {
        var router = new ModeRouter();
        var mode = router.DetectMode(new[] { arg });
        Assert.Equal(expected, mode);
    }

    [Fact]
    public void HandleUnimplementedMode_ReturnsSystemError()
    {
        var router = new ModeRouter();
        var exitCode = router.HandleUnimplementedMode("Test", "Test description");
        Assert.Equal(Constants.ExitCodeSystemError, exitCode);
    }
}
