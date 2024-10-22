// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KM.Abstractions.UnitTests.Diagnostics;

public sealed class SensitiveDataLoggerTests : IDisposable
{
    private const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

    [Fact]
    public void ItIsDisabledByDefault()
    {
        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
    }

    [Fact]
    [Trait("Category", "UnitTest")]
    public void ItCanBeEnabledInDevelopmentEnvironment()
    {
    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development")]
    [InlineData("Development")]
    public void ItCanBeEnabledInDevelopmentEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);
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

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
        SensitiveDataLogger.Enabled = false;
    }
}
