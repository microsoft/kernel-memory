// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KM.Abstractions.UnitTests.Diagnostics;

public sealed class SensitiveDataLoggerTests : IDisposable
{
    private const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItCanBeEnabledInDevelopmentEnvironment()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, "Development");
        SensitiveDataLogger.Enabled = true;
        Assert.True(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    [Trait("Category", "UnitTest")]
    public void ItCannotBeEnabledInNonDevelopmentEnvironments(string environment)
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);
        Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    [Trait("Category", "UnitTest")]
    public void ItCanBeDisabledForAllEnvironments(string environment)
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);
        SensitiveDataLogger.Enabled = false;
        Assert.False(SensitiveDataLogger.Enabled);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
}
