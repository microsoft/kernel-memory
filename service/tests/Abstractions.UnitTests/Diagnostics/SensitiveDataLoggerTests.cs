// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.Diagnostics;

namespace Microsoft.KM.Abstractions.UnitTests.Diagnostics;

public sealed class SensitiveDataLoggerTests : IDisposable
{
    private const string AspNetCoreEnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironmentVariableName = "DOTNET_ENVIRONMENT";

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
    public void ItCanBeEnabledInAspNetCoreDevelopmentEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(AspNetCoreEnvironmentVariableName, environment);

        // Act
        SensitiveDataLogger.Enabled = true;

        // Assert
        Assert.True(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development")]
    [InlineData("Development")]
    public void ItCanBeEnabledInDotNetDevelopmentEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, environment);

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
    public void ItCannotBeEnabledInNonAspNetCoreDevelopmentEnvironments(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(AspNetCoreEnvironmentVariableName, environment);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("staging")]
    [InlineData("Staging")]
    [InlineData("production")]
    [InlineData("Production")]
    [InlineData("any")]
    public void ItCannotBeEnabledInNonDotNetDevelopmentEnvironments(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, environment);

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
    public void ItCanBeDisabledInAnyAspNetCoreEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(AspNetCoreEnvironmentVariableName, environment);

        // Act
        SensitiveDataLogger.Enabled = false;

        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
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
    public void ItCanBeDisabledInAnyDotNetEnvironment(string environment)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, environment);

        // Act
        SensitiveDataLogger.Enabled = false;

        // Assert
        Assert.False(SensitiveDataLogger.Enabled);
    }

    [Theory]
    [Trait("Category", "UnitTest")]
    [InlineData("development", "staging", false)]
    [InlineData("Development", "Staging", false)]
    [InlineData("development", "production", false)]
    [InlineData("Development", "Production", false)]
    [InlineData("staging", "development", true)]
    [InlineData("Staging", "Development", true)]
    [InlineData("production", "development", true)]
    [InlineData("Production", "Development", true)]
    public void AspNetCoreEnvironmentHasThePrecedence(string aspNetCoreEnvironment, string dotNetEnvironment, bool fail)
    {
        // Arrange
        Assert.False(SensitiveDataLogger.Enabled);
        Environment.SetEnvironmentVariable(AspNetCoreEnvironmentVariableName, aspNetCoreEnvironment);
        Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, dotNetEnvironment);

        if (fail)
        {
            Assert.Throws<InvalidOperationException>(() => SensitiveDataLogger.Enabled = true);
        }
        else
        {
            // Act
            SensitiveDataLogger.Enabled = true;

            // Assert
            Assert.True(SensitiveDataLogger.Enabled);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AspNetCoreEnvironmentVariableName, null);
        Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, null);
        SensitiveDataLogger.Enabled = false;
    }
}
