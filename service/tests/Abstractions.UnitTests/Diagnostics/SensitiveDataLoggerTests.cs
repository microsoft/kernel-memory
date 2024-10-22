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

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development")]
    [InlineData("Development")]
    public void ItCanBeEnabledInDevelopmentEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);

        // Act
        SensitiveDataLogger.Enabled = true;

        // Assert
        Assert.True(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("staging")]
    [InlineData("Staging")]
    [InlineData("production")]
    [InlineData("Production")]
    [InlineData("any")]
    public void ItCannotBeEnabledInNonDevelopmentEnvironments(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development")]
    [InlineData("Development")]
    [InlineData("staging")]
    [InlineData("Staging")]
    [InlineData("production")]
    [InlineData("Production")]
    [InlineData("any")]
    public void ItCanBeDisabledInAnyEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(EnvironmentVariableName, environment);

        // Act
        SensitiveDataLogger.Enabled = false;

        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
        SensitiveDataLogger.Enabled = false;
    }
}
